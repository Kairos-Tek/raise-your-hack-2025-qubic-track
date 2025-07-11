import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-progress-bar',
    templateUrl: './progress-bar.component.html',
})
export class ProgressBarComponent {
    @Input() progress: number = 0;
}
