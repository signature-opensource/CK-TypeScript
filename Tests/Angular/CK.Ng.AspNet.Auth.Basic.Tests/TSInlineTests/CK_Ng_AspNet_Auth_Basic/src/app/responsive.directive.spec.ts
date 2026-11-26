import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { Observable, Subject } from 'rxjs';
import { ResponsiveDirective } from '@local/ck-gen';

class FakeBreakpointObserver {
  readonly queries = new Map<string, Subject<BreakpointState>>();

  observe( query: string ): Observable<BreakpointState> {
    let subject = this.queries.get( query );
    if( !subject ) {
      subject = new Subject<BreakpointState>();
      this.queries.set( query, subject );
    }
    return subject;
  }

  emit( query: string, matches: boolean ): void {
    this.queries.get( query )!.next( { matches, breakpoints: { [query]: matches } } );
  }
}

@Component( {
  imports: [ResponsiveDirective],
  template: `<span class="probe" *appResponsive="query()">content</span>`
} )
class Host {
  readonly query = signal( '(max-width: 1024px)' );
}

describe( 'ResponsiveDirective', () => {
  const mobile = '(max-width: 1024px)';
  const narrow = '(max-width: 600px)';
  let fixture: ComponentFixture<Host>;
  let observer: FakeBreakpointObserver;

  beforeEach( async () => {
    observer = new FakeBreakpointObserver();
    await TestBed.configureTestingModule( {
      imports: [Host],
      providers: [{ provide: BreakpointObserver, useValue: observer }]
    } ).compileComponents();
    fixture = TestBed.createComponent( Host );
    fixture.detectChanges();
  } );

  function probeCount(): number {
    fixture.detectChanges();
    return fixture.nativeElement.querySelectorAll( '.probe' ).length;
  }

  it( 'shows the content once however many times the query matches, and hides it when the query stops matching', () => {
    observer.emit( mobile, true );
    observer.emit( mobile, true );
    expect( probeCount() ).toBe( 1 );
    observer.emit( mobile, false );
    expect( probeCount() ).toBe( 0 );
  } );

  it( 'stops following a breakpoint the input no longer names', () => {
    fixture.componentInstance.query.set( narrow );
    fixture.detectChanges();
    expect( observer.queries.get( mobile )!.observed ).toBe( false );
    observer.emit( mobile, true );
    expect( probeCount() ).toBe( 0 );
  } );

  it( 'stops following the breakpoint when destroyed', () => {
    expect( observer.queries.get( mobile )!.observed ).toBe( true );
    fixture.destroy();
    expect( observer.queries.get( mobile )!.observed ).toBe( false );
  } );
} );
