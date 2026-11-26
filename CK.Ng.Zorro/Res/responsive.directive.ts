import { DestroyRef, Directive, inject, Input, TemplateRef, ViewContainerRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { BreakpointObserver } from '@angular/cdk/layout';
import { Subscription } from 'rxjs';

@Directive( {
  selector: '[appResponsive]'
} )
export class ResponsiveDirective {
  #breakpointObserver = inject( BreakpointObserver );
  #templateRef = inject( TemplateRef );
  #viewContainer = inject( ViewContainerRef );
  #destroyRef = inject( DestroyRef );
  #subscription?: Subscription;
  #hasView = false;

  @Input() set appResponsive( breakpoint: string ) {
    this.#subscription?.unsubscribe();
    this.#subscription = this.#breakpointObserver.observe( breakpoint )
      .pipe( takeUntilDestroyed( this.#destroyRef ) )
      .subscribe( result => {
        if ( result.matches && !this.#hasView ) {
          this.#viewContainer.createEmbeddedView( this.#templateRef );
          this.#hasView = true;
        } else if ( !result.matches && this.#hasView ) {
          this.#viewContainer.clear();
          this.#hasView = false;
        }
      } );
  }
}
