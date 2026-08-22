import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('creates the application shell', () => {
    const fixture = TestBed.createComponent(AppComponent);

    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders the primary navigation entries', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const labels = Array.from(
      fixture.nativeElement.querySelectorAll('nav a') as NodeListOf<HTMLAnchorElement>
    ).map(link => link.textContent?.trim());

    expect(labels).toContain('Korp ERP');
    expect(labels).toContain('Produtos');
    expect(labels).toContain('Notas Fiscais');
  });

  it('renders the current year in the footer', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const footer = fixture.nativeElement.querySelector('footer') as HTMLElement | null;

    expect(footer?.textContent).toContain(String(new Date().getFullYear()));
    expect(footer?.textContent).toContain('Sistema de Emissão de Notas Fiscais');
  });
});
