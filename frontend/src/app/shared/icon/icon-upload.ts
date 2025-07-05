import { Component, Input, ViewChild, ViewContainerRef } from '@angular/core';
@Component({
    moduleId: module.id,
    selector: 'icon-upload',
    template: `
        <ng-template #template>
            <svg width="43" height="43" viewBox="0 0 43 43" fill="none" [ngClass]="class" xmlns="http://www.w3.org/2000/svg">
                <path d="M21.5 23.2915V37.6248" stroke="currentColor" stroke-width="4" stroke-linecap="round" stroke-linejoin="round" />
                <path
                    d="M7.1667 26.6941C5.83557 25.3341 4.83139 23.689 4.23022 21.8834C3.62906 20.0778 3.44668 18.1591 3.69689 16.2726C3.94711 14.386 4.62336 12.5812 5.67442 10.9947C6.72548 9.40827 8.12378 8.08181 9.76343 7.1158C11.4031 6.1498 13.241 5.5696 15.1381 5.41914C17.0352 5.26869 18.9417 5.55192 20.7131 6.2474C22.4845 6.94287 24.0744 8.03235 25.3624 9.4333C26.6504 10.8343 27.6027 12.5099 28.1471 14.3334H31.3542C33.0841 14.3332 34.7682 14.8894 36.1576 15.9199C37.5471 16.9503 38.5684 18.4003 39.0705 20.0557C39.5726 21.7111 39.529 23.4841 38.9461 25.1128C38.3632 26.7415 37.2719 28.1395 35.8334 29.1004"
                    stroke="currentColor"
                    stroke-width="4"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                />
                <path d="M14.3334 30.4582L21.5 23.2915L28.6667 30.4582" stroke="currentColor" stroke-width="4" stroke-linecap="round" stroke-linejoin="round" />
            </svg>
        </ng-template>
    `,
})
export class IconUploadComponent {
    @Input() fill: boolean = false;
    @Input() class: any = '';
    @ViewChild('template', { static: true }) template: any;
    constructor(private viewContainerRef: ViewContainerRef) {}
    ngOnInit() {
        this.viewContainerRef.createEmbeddedView(this.template);
        this.viewContainerRef.element.nativeElement.remove();
    }
}
