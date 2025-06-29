import { Component, Inject, ViewChild, AfterViewInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { BrowserAutomationProfileBuilderComponent } from './browser-automation-profile-builder.component';

@Component({
  selector: 'app-browser-automation-profile-dialog',
  templateUrl: './browser-automation-profile-dialog.component.html',
  styleUrls: ['./browser-automation-profile-dialog.component.scss'],
  standalone: false,
})
export class BrowserAutomationProfileDialogComponent implements AfterViewInit {
  @ViewChild('profileBuilder') profileBuilder!: BrowserAutomationProfileBuilderComponent;
  isMobile = false;

  constructor(
    public dialogRef: MatDialogRef<BrowserAutomationProfileDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    breakpointObserver: BreakpointObserver
  ) {
    breakpointObserver.observe([Breakpoints.Handset]).subscribe(result => {
      this.isMobile = result.matches;
      if (this.isMobile) {
        dialogRef.updateSize('100vw', '100vh');
        dialogRef.addPanelClass('full-screen-dialog');
      }
    });
  }

  ngAfterViewInit() {
    // Set the initial profile data when the view is ready
    if (this.data.profile && this.profileBuilder) {
      this.profileBuilder.writeValue(this.data.profile);
    }
  }

  save() {
    // Get the current profile from the builder
    const currentProfile = this.profileBuilder.profileForm.value;
    this.dialogRef.close(currentProfile);
  }

  cancel() {
    this.dialogRef.close(null);
  }
} 