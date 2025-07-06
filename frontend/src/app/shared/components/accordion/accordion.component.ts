import { Component, Input } from '@angular/core';
import { slideDownUp } from '../../animations';

@Component({
  selector: 'app-accordion',
  templateUrl: './accordion.component.html',
  animations: [slideDownUp],
})
export class AccordionComponent {

  @Input() title: string = '';
  @Input() totalTests: number = 0;
  open = false;

  toggle() {
    this.open = !this.open;
  }
}
