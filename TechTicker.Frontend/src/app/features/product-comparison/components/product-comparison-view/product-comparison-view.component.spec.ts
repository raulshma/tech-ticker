import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProductComparisonViewComponent } from './product-comparison-view.component';

describe('ProductComparisonViewComponent', () => {
  let component: ProductComparisonViewComponent;
  let fixture: ComponentFixture<ProductComparisonViewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ProductComparisonViewComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ProductComparisonViewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});