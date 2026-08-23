import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './home-page.component.html',
  styles: `
    .mask-gradient {
      mask-image: linear-gradient(to right, transparent, black 15%, black 100%);
      -webkit-mask-image: linear-gradient(to right, transparent, black 15%, black 100%);
    }

    /* Animation classes */
    .slide-enter {
      transform: translateX(100%);
      opacity: 0;
    }
    .slide-enter-active {
      transform: translateX(0);
      opacity: 1;
      transition: transform 0.5s ease-out, opacity 0.5s ease-out;
    }
    
    .slide-leave {
      transform: translateX(0);
      opacity: 1;
    }
    .slide-leave-active {
      transform: translateX(-100%);
      opacity: 0;
      transition: transform 0.5s ease-in, opacity 0.5s ease-in;
    }
  `
})
export class HomePageComponent implements OnInit, OnDestroy {
  // Images to slide
  images = [
    { src: '/assets/erp2.png?v=2', alt: 'ERP Dashboard 1' },
    { src: '/assets/erp4.png?v=2', alt: 'ERP Dashboard 2' }
  ];

  currentIndex = signal(0);
  private intervalId: any;

  ngOnInit() {
    this.intervalId = setInterval(() => {
      this.currentIndex.update(index => (index + 1) % this.images.length);
    }, 2000); // 2 seconds total per slide (1s visible, 1s sliding approximately, or rather just switch every 2s to allow animation to complete nicely)
  }

  ngOnDestroy() {
    if (this.intervalId) {
      clearInterval(this.intervalId);
    }
  }
}
