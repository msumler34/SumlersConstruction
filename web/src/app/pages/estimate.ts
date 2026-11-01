import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

interface PriceGuide {
  type: string;
  label: string;
  low: number;
  high: number;
  note: string;
}

@Component({
  selector: 'app-estimate',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, CommonModule],
  styles: `
    :host { display: block; }
    .wrap { max-width: 720px; margin: 2rem auto; padding: 0 1rem; }
    h1 { font-size: 1.8rem; margin-bottom: 0.5rem; }
    .lead { color: #555; margin-bottom: 1.5rem; }
    label { display: block; font-weight: 600; margin: 1rem 0 0.3rem; }
    select, input, textarea { width: 100%; padding: 0.6rem; border: 1px solid #ccc; border-radius: 6px; font-size: 1rem; }
    .preview { margin: 1.5rem 0; padding: 1rem; background: #f7f7f7; border-radius: 8px; }
    .range { font-size: 1.6rem; font-weight: 700; color: #c9803a; }
    .note { font-size: 0.9rem; color: #666; margin-top: 0.4rem; }
    button { background: #c9803a; color: white; border: none; padding: 0.8rem 1.4rem; border-radius: 6px; font-size: 1rem; cursor: pointer; margin-top: 1rem; }
    button:disabled { opacity: 0.5; }
    .success { background: #e8f5e9; padding: 1rem; border-radius: 8px; margin-top: 1rem; }
    .contact-note { font-size: 0.9rem; color: #666; margin: 0.3rem 0 0.8rem; }
  `,
  template: `
    <div class="wrap">
      <h1>Get a rough remodel estimate</h1>
      <p class="lead">Upload a photo of your space and get a ballpark price range plus a visual idea of what a remodel could look like.</p>

      <label>Project type</label>
      <select [(ngModel)]="projectType">
        <option value="">Choose one</option>
        @for (g of guides; track g.type) {
          <option [value]="g.type">{{ g.label }}</option>
        }
      </select>

      <label>Photo of your current space (optional)</label>
      <input type="file" accept="image/*" (change)="onFile($event)" />

      @if (previewUrl()) {
        <div class="preview">
          <img [src]="previewUrl()" alt="Your photo" style="max-width:100%; border-radius:6px;" />
          <p class="note">We’ll use this to generate a remodel visualization.</p>
        </div>
      }

      @if (selectedGuide()) {
        <div class="preview">
          <div class="range">{{ selectedGuide()!.low | currency:'USD':'symbol':'1.0-0' }} – {{ selectedGuide()!.high | currency:'USD':'symbol':'1.0-0' }}</div>
          <p class="note">{{ selectedGuide()!.note }}</p>
          <p class="note">This is a rough range based on past jobs. A free on-site estimate is still required for an exact price.</p>
        </div>
      }

      <label>Your name (optional)</label>
      <input type="text" [(ngModel)]="name" />

      <p class="contact-note">How should we reach you? Just fill in one.</p>

      <label>Phone (for text)</label>
      <input type="tel" [(ngModel)]="phone" placeholder="(816) 555-1234" />

      <label>Email</label>
      <input type="email" [(ngModel)]="email" placeholder="you@email.com" />

      <label>Anything else we should know? (optional)</label>
      <textarea rows="3" [(ngModel)]="notes"></textarea>

      <button (click)="submit()" [disabled]="!canSubmit()">Get my estimate</button>

      @if (submitted()) {
        <div class="success">
          Thanks! Your details have been sent. We’ll follow up soon.
        </div>
      }
    </div>
  `,
})
export class Estimate {
  projectType = '';
  name = '';
  phone = '';
  email = '';
  notes = '';
  previewUrl = signal<string | null>(null);
  submitted = signal(false);

  guides: PriceGuide[] = [
    { type: 'bathroom', label: 'Bathroom remodel', low: 8500, high: 12000, note: 'Typical 5×8 bathroom, 2 guys × 4 days.' },
    { type: 'master-bath', label: 'Master bathroom', low: 18000, high: 28000, note: 'Larger scope, often around $25k.' },
    { type: 'deck', label: 'Deck', low: 60, high: 95, note: 'Per square foot. Basic to composite + metal rail.' },
    { type: 'covered-deck', label: 'Covered deck / roof structure', low: 12000, high: 20000, note: 'Added to deck cost.' },
    { type: 'basement', label: 'Full basement', low: 45000, high: 65000, note: 'Includes layout; kitchen/bar and bath extra.' },
    { type: 'kitchen', label: 'Kitchen remodel', low: 15000, high: 40000, note: 'Materials × 2 rule of thumb. Cabinets drive most of the cost.' },
    { type: 'painting', label: 'Exterior painting', low: 5000, high: 9000, note: 'Whole-house painting range.' },
    { type: 'siding', label: 'Siding', low: 6000, high: 15000, note: 'Repair to front replacement.' },
  ];

  selectedGuide() {
    return this.guides.find((g) => g.type === this.projectType) ?? null;
  }

  onFile(event: Event) {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => this.previewUrl.set(reader.result as string);
    reader.readAsDataURL(file);
  }

  canSubmit() {
    return this.projectType && (this.phone || this.email);
  }

  submit() {
    const guide = this.selectedGuide();
    const subject = encodeURIComponent('Rough Estimate Request');
    const body = encodeURIComponent(
      `Name: ${this.name || 'Not given'}\n` +
      `Project: ${guide?.label || this.projectType}\n` +
      `Range: \[ {guide?.low.toLocaleString()} – \]{guide?.high.toLocaleString()}\n` +
      `Phone: ${this.phone || 'Not given'}\n` +
      `Email: ${this.email || 'Not given'}\n` +
      `Notes: ${this.notes || 'None'}`
    );

    if (this.email) {
      window.location.href = `mailto:mattsumler@gmail.com?subject=\( {subject}&body= \){body}`;
    } else if (this.phone) {
      window.location.href = `sms:8163042469?body=${body}`;
    }

    this.submitted.set(true);
  }
}