import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, ElementRef, effect, inject, input, signal, viewChild } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ContactService } from '../../core/contact.service';
import { SiteService } from '../../core/site.service';
import { Icon } from '../../shared/icon';
import { PageHero } from '../page-hero';

type SendState = 'idle' | 'sending' | 'sent' | 'error';

@Component({
  selector: 'app-contact',
  imports: [ReactiveFormsModule, Icon, PageHero],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './contact.html',
  styleUrl: './contact.scss',
})
export class Contact {
  /** Pre-selects the service, from links such as /contact?service=Custom%20Decks. */
  readonly service = input<string>();

  protected readonly siteService = inject(SiteService);
  private readonly contactService = inject(ContactService);

  protected readonly heroImage =
    'https://images.unsplash.com/photo-1600596542815-ffad4c1539a9?auto=format&fit=crop&w=2000&q=80';
  private readonly formCard = viewChild<ElementRef<HTMLElement>>('formCard');
  protected readonly state = signal<SendState>('idle');
  protected readonly errorMessage = signal('');

  protected readonly form = inject(NonNullableFormBuilder).group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
    phone: ['', Validators.maxLength(40)],
    service: [''],
    address: ['', Validators.maxLength(200)],
    message: ['', [Validators.required, Validators.maxLength(4000)]],
    website: [''],
  });

  constructor() {
    effect(() => {
      const service = this.service();
      if (service) this.form.controls.service.setValue(service);
    });
  }

  protected showError(control: 'name' | 'email' | 'message'): boolean {
    const c = this.form.controls[control];
    return c.invalid && (c.touched || c.dirty);
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.state.set('sending');
    this.contactService.send(this.form.getRawValue()).subscribe({
      next: () => {
        this.state.set('sent');
        this.form.reset();
        this.formCard()?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
      },
      error: (err: HttpErrorResponse) => {
        this.errorMessage.set(messageFor(err));
        this.state.set('error');
      },
    });
  }

  protected startOver(): void {
    this.state.set('idle');
  }
}

function messageFor(err: HttpErrorResponse): string {
  if (err.status === 429) return 'You have sent several messages in a short time. Please wait a few minutes, or call us.';
  if (err.status === 400) return 'Please check the form and try again.';
  return 'Something went wrong sending your message. Please call us or send us a message on Facebook.';
}
