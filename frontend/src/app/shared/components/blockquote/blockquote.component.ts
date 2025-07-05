import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-blockquote',
    templateUrl: './blockquote.component.html',
})
export class BlockquoteComponent {
    @Input() title: string = '';
    @Input() content: string = '';
}
