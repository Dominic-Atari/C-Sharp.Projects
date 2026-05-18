import { Component } from '@angular/core';

// Minimal stub component retained so builds referencing the file do not break.
@Component({
  selector: 'app-drawing-canvas',
  template: `<div class="drawing-stub">Drawing disabled</div>`,
  styles: [`.drawing-stub{padding:12px;color:#666;border:1px dashed #ccc;border-radius:6px;text-align:center;}`]
})
export class DrawingCanvasComponent {
  // intentionally empty — drawing integration removed per user request
}

