import { Directive, inject, Input, TemplateRef, ViewContainerRef } from '@angular/core';
import { BreakpointObserver } from '@angular/cdk/layout';

@Directive( {
  selector: '[appResponsive]'
} )
export class ResponsiveDirective {
  #breakpointObserver = inject( BreakpointObserver );
  #templateRef = inject( TemplateRef );
  #viewContainer = inject( ViewContainerRef );

  @Input() set appResponsive( breakpoint: string ) {
    this.#breakpointObserver.observe( breakpoint ).subscribe( result => {
      if ( result.matches ) {
        this.#viewContainer.createEmbeddedView( this.#templateRef );
      } else {
        this.#viewContainer.clear();
      }
    } );
  }
}
