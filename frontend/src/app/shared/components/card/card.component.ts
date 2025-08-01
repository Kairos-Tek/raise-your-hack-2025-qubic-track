import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-card',
    templateUrl: './card.component.html',
})
export class CardComponent {
    @Input() subtitle: string = '';
    @Input() title: string = '';
    @Input() description: string = '';
}
