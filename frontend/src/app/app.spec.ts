import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      // App chỉ chứa <router-outlet />, nên phải có Router mới dựng được.
      providers: [provideRouter(routes)],
    }).compileComponents();
  });

  it('dựng được component gốc', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
