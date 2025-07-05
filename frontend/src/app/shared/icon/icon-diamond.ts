import { Component, Input, ViewChild, ViewContainerRef } from '@angular/core';
@Component({
    moduleId: module.id,
    selector: 'icon-diamond',
    template: `
        <ng-template #template>
            <svg width="16" height="17" viewBox="0 0 16 17" fill="none" [ngClass]="class" xmlns="http://www.w3.org/2000/svg">
                <path
                    d="M3.33356 15.1668H12.6669M1.66689 5.8335H14.3336M6.66689 1.8335L5.33356 5.8335L8.00022 12.8335L10.6669 5.8335L9.33356 1.8335M8.39486 12.7327L14.3546 6.17703C14.4653 6.05526 14.5206 5.99437 14.5427 5.92504C14.5621 5.86395 14.5635 5.79855 14.5468 5.73668C14.5277 5.66647 14.475 5.60326 14.3697 5.47684L11.4935 2.0254C11.4347 1.95486 11.4053 1.91959 11.3693 1.89423C11.3374 1.87177 11.3017 1.85508 11.264 1.84494C11.2215 1.8335 11.1756 1.8335 11.0838 1.8335H4.91669C4.82487 1.8335 4.77896 1.8335 4.73642 1.84494C4.69872 1.85508 4.6631 1.87177 4.63118 1.89423C4.59515 1.91959 4.56576 1.95486 4.50697 2.0254L1.63077 5.47683C1.52542 5.60326 1.47274 5.66647 1.45369 5.73668C1.4369 5.79855 1.43831 5.86395 1.45775 5.92504C1.47981 5.99437 1.53516 6.05525 1.64586 6.17702L7.60559 12.7327C7.7424 12.8832 7.81081 12.9585 7.8912 12.9863C7.96183 13.0107 8.03862 13.0107 8.10924 12.9863C8.18964 12.9585 8.25805 12.8832 8.39486 12.7327Z"
                    stroke="currentColor"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                />
            </svg>
        </ng-template>
    `,
})
export class IconDiamondComponent {
    @Input() fill: boolean = false;
    @Input() class: any = '';
    @ViewChild('template', { static: true }) template: any;
    constructor(private viewContainerRef: ViewContainerRef) {}
    ngOnInit() {
        this.viewContainerRef.createEmbeddedView(this.template);
        this.viewContainerRef.element.nativeElement.remove();
    }
}
