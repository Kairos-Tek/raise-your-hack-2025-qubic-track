import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-primary-button',
    templateUrl: './primary-button.component.html',
})
export class PrimaryButtonComponent {
    @Input() disabled = false;
    @Input() loading = false;
    @Input() text = 'Submit';
    @Input() type: 'button' | 'submit' = 'button';
}
