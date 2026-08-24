import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  NgZone,
  computed,
  effect,
  inject,
  viewChild,
} from '@angular/core';
import { ThemeService } from '../../../core/theme/theme.service';

/** Một chấm trôi. */
interface Node {
  x: number;
  y: number;
  vx: number;
  vy: number;
  r: number;
}

/**
 * Nền động của màn đăng nhập: <b>sơ đồ tổ chức trôi chậm</b>.
 *
 * Các chấm nối nhau chính là thứ sản phẩm nói về — cây phòng ban và người trong công ty
 * — chứ không phải hoa văn trang trí bất kỳ. Thuật toán chép đúng từ bản dựng đã duyệt
 * `docs/07-giao-dien/identity/dang-nhap.html`, kể cả mấy con số: ngưỡng nối 168px, mật
 * độ một chấm trên 26000 px², vận tốc ±0.14.
 *
 * <b>Vì sao canvas chứ không phải SVG:</b> mật độ chấm tính theo diện tích, và mỗi khung
 * hình phải xét mọi CẶP chấm để quyết định có kẻ đường nối hay không. Với SVG thì mỗi
 * đường nối là một phần tử DOM sinh ra rồi bỏ đi 60 lần một giây — trình duyệt sẽ bò.
 * Canvas chỉ là một bức ảnh được vẽ lại.
 */
@Component({
  selector: 'app-org-weave',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<canvas #canvas aria-hidden="true"></canvas>`,
  styles: `
    :host {
      position: absolute;
      inset: 0;
    }

    canvas {
      display: block;
      width: 100%;
      height: 100%;
    }
  `,
})
export class OrgWeave implements AfterViewInit {
  // KHÔNG dùng viewChild.required: effect trong constructor chạy TRƯỚC khi view dựng
  // xong, và bản required sẽ ném lỗi ở lần chạy đầu tiên đó.
  private readonly canvasRef = viewChild<ElementRef<HTMLCanvasElement>>('canvas');
  private readonly zone = inject(NgZone);
  private readonly theme = inject(ThemeService);
  private readonly destroyRef = inject(DestroyRef);

  private nodes: Node[] = [];
  private width = 0;
  private height = 0;
  private frame = 0;

  /**
   * Máy đã xin giảm chuyển động thì vẽ MỘT khung rồi đứng yên hẳn.
   *
   * Không phải "chậm lại" — với người bị rối loạn tiền đình, một chuyển động nhỏ vẫn đủ
   * gây chóng mặt. Hình vẫn còn đó, chỉ là không nhúc nhích.
   */
  private readonly still = computed(
    () =>
      typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches,
  );

  constructor() {
    // Đổi bộ màu thì vẽ lại ngay bằng màu mới. Không có chỗ này thì nền giữ nguyên màu
    // cũ cho tới khung hình sau — và ở chế độ đứng yên thì giữ mãi mãi.
    effect(() => {
      this.theme.current();
      this.paint();
    });
  }

  ngAfterViewInit(): void {
    const onResize = () => this.start();

    /**
     * Chạy NGOÀI vùng theo dõi của Angular.
     *
     * `requestAnimationFrame` bên trong vùng đó sẽ kích hoạt một vòng dò thay đổi mỗi
     * khung hình — 60 lần một giây, cho một cái nền không hề đọc dữ liệu nào của app.
     * Đó là cách chắc chắn nhất để một trang tĩnh ngốn hết pin máy người dùng.
     */
    this.zone.runOutsideAngular(() => {
      this.start();
      addEventListener('resize', onResize);
    });

    this.destroyRef.onDestroy(() => {
      cancelAnimationFrame(this.frame);
      removeEventListener('resize', onResize);
    });
  }

  private start(): void {
    cancelAnimationFrame(this.frame);
    this.seed();

    if (this.still()) {
      this.paint();
    } else {
      this.step();
    }
  }

  private seed(): void {
    const canvas = this.canvasRef()?.nativeElement;

    if (!canvas) {
      return;
    }

    const box = canvas.getBoundingClientRect();

    // Giới hạn ở 2: màn hình 3x chỉ làm canvas nặng gấp 2.25 lần mà mắt không phân biệt
    // được thêm gì trên một cái nền mờ 24%.
    const dpr = Math.min(devicePixelRatio || 1, 2);

    this.width = box.width;
    this.height = box.height;

    canvas.width = this.width * dpr;
    canvas.height = this.height * dpr;

    canvas.getContext('2d')?.setTransform(dpr, 0, 0, dpr, 0, 0);

    // Mật độ theo DIỆN TÍCH, không phải số cố định: cùng một con số trên màn 13 inch thì
    // rối, trên màn 32 inch thì thưa thớt như bị lỗi.
    const count = Math.max(14, Math.round((this.width * this.height) / 26000));

    this.nodes = Array.from({ length: count }, () => ({
      x: Math.random() * this.width,
      y: Math.random() * this.height,
      vx: (Math.random() - 0.5) * 0.14,
      vy: (Math.random() - 0.5) * 0.14,
      r: Math.random() * 1.6 + 1.4,
    }));
  }

  private paint(): void {
    const canvas = this.canvasRef()?.nativeElement;
    const context = canvas?.getContext('2d');

    if (!canvas || !context || this.nodes.length === 0) {
      return;
    }

    // Đọc màu từ biến CSS đang có hiệu lực, nên nền tự đúng màu ở cả bốn bộ mà không
    // cần biết bộ nào đang bật.
    const ink = getComputedStyle(document.documentElement).getPropertyValue('--ink-faint').trim();

    context.clearRect(0, 0, this.width, this.height);
    context.strokeStyle = ink;

    // Nối mọi cặp chấm gần nhau hơn 168px, càng gần càng đậm. Đây là chỗ hình thù "sơ đồ
    // tổ chức" hiện ra: các cụm tự gom lại và tự tan ra khi chấm trôi.
    for (let i = 0; i < this.nodes.length; i++) {
      for (let j = i + 1; j < this.nodes.length; j++) {
        const distance = Math.hypot(
          this.nodes[i].x - this.nodes[j].x,
          this.nodes[i].y - this.nodes[j].y,
        );

        if (distance > 168) {
          continue;
        }

        context.globalAlpha = (1 - distance / 168) * 0.24;
        context.beginPath();
        context.moveTo(this.nodes[i].x, this.nodes[i].y);
        context.lineTo(this.nodes[j].x, this.nodes[j].y);
        context.stroke();
      }
    }

    context.fillStyle = ink;

    for (const node of this.nodes) {
      context.globalAlpha = 0.45;
      context.beginPath();
      context.arc(node.x, node.y, node.r, 0, Math.PI * 2);
      context.fill();
    }

    context.globalAlpha = 1;
  }

  private step = (): void => {
    for (const node of this.nodes) {
      node.x += node.vx;
      node.y += node.vy;

      // Nảy lại ở mép thay vì cuộn vòng: cuộn vòng làm chấm biến mất một bên rồi hiện ra
      // bên kia, và mắt bắt được cú nhảy đó ngay.
      if (node.x < 0 || node.x > this.width) {
        node.vx *= -1;
      }
      if (node.y < 0 || node.y > this.height) {
        node.vy *= -1;
      }
    }

    this.paint();
    this.frame = requestAnimationFrame(this.step);
  };
}
