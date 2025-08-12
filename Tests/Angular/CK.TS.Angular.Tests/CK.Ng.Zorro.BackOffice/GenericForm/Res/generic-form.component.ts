import { CommonModule } from '@angular/common';
import { Component, effect, HostListener, inject, input, output, signal, WritableSignal } from '@angular/core';
import { AbstractControl, AbstractControlOptions, FormBuilder, FormControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';

import { IFormControlConfig, GenericFormData } from './generic-form.model';

import { NzCheckboxModule } from 'ng-zorro-antd/checkbox';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzFormLayoutType, NzFormModule } from 'ng-zorro-antd/form';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NZ_MODAL_DATA, NzModalRef } from 'ng-zorro-antd/modal';
import { NzSelectModule } from 'ng-zorro-antd/select';

@Component( {
  selector: 'ck-generic-form',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, NzDatePickerModule, NzCheckboxModule, NzFormModule, NzInputModule, NzSelectModule],
  templateUrl: './generic-form.component.html'
} )
export class GenericFormComponent {
  @HostListener( 'document:keydown.enter', ['$event'] )
  keyEvent( event: Event ) {
    if ( this.#modalRef ) {
      this.#modalRef.triggerOk();
    } else {
      this.submitRequested.emit();
    }
    event.preventDefault();
    event.stopPropagation();
  }

  inputFormData = input.required<GenericFormData<unknown, unknown>>();
  formLayout = input<NzFormLayoutType>( 'vertical' );
  formGroup = input<FormGroup>();
  submitRequested = output<void>();

  readonly #modalData = inject( NZ_MODAL_DATA, { optional: true } );
  readonly #formBuilder = inject( FormBuilder );
  readonly #modalRef = inject( NzModalRef, { optional: true } );

  form: WritableSignal<FormGroup | undefined> = signal( undefined );
  formData?: GenericFormData<unknown, unknown>;

  constructor() {
    if ( this.#modalData ) {
      this.formData = this.#modalData.formData;
      this.form.set( this.createFormGroup() );
    }

    effect( () => {
      if ( !this.#modalData ) {
        this.formData = this.inputFormData();
        this.form.set( this.createFormGroup() );
      }
    } );
  }

  createFormGroup(): FormGroup {
    const group: { [key: string]: AbstractControl } = {};

    for ( const key in this.formData!.formControls ) {
      if ( this.formData!.formControls.hasOwnProperty( key ) ) {
        const controlConfig = this.formData!.formControls[key];
        const validators = controlConfig.validators ?? [];
        group[key] = new FormControl( { value: controlConfig.defaultValue, disabled: controlConfig.disabled }, { nonNullable: controlConfig.required ?? false, validators: validators } );
      }
    }

    const groupOpts: AbstractControlOptions = {};

    if ( this.formData ) {
      if ( this.formData.generalFormValidators ) {
        groupOpts.validators = this.formData.generalFormValidators.validators;
      }
      // if ( this.formData.generalFormAsyncValidators ) {
      //   groupOpts.asyncValidators = this.formData.generalFormAsyncValidators.validators;
      // }
    }

    return this.#formBuilder.group( group, groupOpts );
  }

  getFormControl( controlName: string ): FormControl {
    return this.form()?.get( controlName ) as FormControl;
  }

  getErrorMessageKeys( key: string ): string[] {
    const control = this.form()?.get( key );
    return control?.errors ? Object.keys( control.errors ) : [];
  }

  originalOrder = () => 0;

  isVisible( control: IFormControlConfig<unknown, unknown> ): boolean {
    return control.show ? control.show( this.form()!.value ) : true;
  }

  getGeneralFormErrorKeys(): string[] {
    if ( !this.form() || !this.form()!.errors ) return [];

    return Object.keys( this.form()!.errors! );
  }

  getGeneralFormErrorMessage( key: string ): string {
    if ( this.formData ) {
      if ( this.formData.generalFormErrors ) {
        return this.formData.generalFormErrors[key] ?? '';
      }
      if ( this.formData.generalFormAsyncValidators ) {
        return this.formData.generalFormAsyncValidators.errorMessages[key] ?? '';
      }
      if ( this.formData.generalFormValidators ) {
        return this.formData.generalFormValidators.errorMessages[key] ?? '';
      }
    }
    return '';
  }

  submitForm( _: Event ): void {
    if ( this.#modalRef ) {
      this.#modalRef.triggerOk();
    }
  }
}
