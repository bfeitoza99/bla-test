import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * Reusable placeholder panel for skeleton routed pages.
 *
 * Renders a titled card with optional descriptive copy and projected content.
 * This is scaffolding UI — real feature components replace these pages in later
 * waves.
 */
@Component({
  selector: 'app-page-placeholder',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  templateUrl: './page-placeholder.html',
  styleUrl: './page-placeholder.scss',
})
export class PagePlaceholder {
  /** Heading shown at the top of the placeholder card. */
  readonly heading = input.required<string>();

  /** Optional supporting line under the heading. */
  readonly description = input<string>('');
}
