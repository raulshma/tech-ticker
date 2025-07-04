import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, takeUntil, debounceTime, distinctUntilChanged, switchMap, forkJoin, of, Observable, map } from 'rxjs';
import { ProductDto } from '../../../../shared/api/api-client';
import { ProductsService } from '../../../products/services/products.service';
import {
  ProductComparisonService,
  ProductComparisonResultDto,
  CompareProductsRequestDto
} from '../../services/product-comparison.service';

@Component({
  selector: 'app-product-comparison-view',
  templateUrl: './product-comparison-view.component.html',
  styleUrls: ['./product-comparison-view.component.scss'],
  standalone: false
})
export class ProductComparisonViewComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // Form controls
  comparisonForm = new FormGroup({
    product1: new FormControl<ProductDto | null>(null, [Validators.required]),
    product2: new FormControl<ProductDto | null>(null, [Validators.required]),
    includePriceAnalysis: new FormControl(true),
    generateRecommendations: new FormControl(true)
  });

  // State
  isLoading = false;
  isComparing = false;
  error: string | null = null;
  comparisonResult: ProductComparisonResultDto | null = null;

  // Product search
  product1SearchControl = new FormControl('');
  product2SearchControl = new FormControl('');
  product1Options: ProductDto[] = [];
  product2Options: ProductDto[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar,
    private productsService: ProductsService,
    private comparisonService: ProductComparisonService
  ) {}

  ngOnInit(): void {
    this.setupProductSearch();
    this.checkRouteParams();

    // Test search functionality by triggering a search immediately
    setTimeout(() => {
      this.product1SearchControl.setValue('a'); // Trigger a search with 'a'
    }, 1000);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupProductSearch(): void {
    // Setup product 1 search
    this.product1SearchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(search => {
        if (!search || search.length < 1) return of({ items: [] } as any);
        return this.productsService.getProducts({ search, pageSize: 20 });
      }),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (result) => {
        this.product1Options = result.items;
      },
      error: (error) => {
        console.error('Error searching products for product 1:', error);
      }
    });

    // Setup product 2 search
    this.product2SearchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(search => {
        if (!search || search.length < 1) return of({ items: [] } as any);
        return this.productsService.getProducts({ search, pageSize: 20 });
      }),
      takeUntil(this.destroy$)
    ).subscribe({
      next: (result) => {
        this.product2Options = result.items;
      },
      error: (error) => {
        console.error('Error searching products for product 2:', error);
      }
    });
  }

  private checkRouteParams(): void {
    this.route.params.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const product1Id = params['productId1'];
      const product2Id = params['productId2'];

      if (product1Id && product2Id) {
        this.loadProductsFromIds(product1Id, product2Id);
      }
    });
  }

  private loadProductsFromIds(product1Id: string, product2Id: string): void {
    this.isLoading = true;
    this.error = null;

    forkJoin({
      product1: this.getProductById(product1Id),
      product2: this.getProductById(product2Id)
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: ({ product1, product2 }) => {
        this.comparisonForm.patchValue({
          product1: product1 as ProductDto,
          product2: product2 as ProductDto
        });
        this.performComparison();
      },
      error: (error) => {
        this.error = 'Failed to load products for comparison';
        this.isLoading = false;
        this.snackBar.open(this.error, 'Close', { duration: 5000 });
      }
    });
  }

  onProduct1Selected(product: ProductDto | null): void {
    if (!product || !product.productId) {
      this.comparisonForm.patchValue({ product1: null });
      this.clearComparison();
      return;
    }

    this.comparisonForm.patchValue({ product1: product });
    this.product1SearchControl.setValue(product.name || '');

    // Load comparable products for product 2
    if (product.productId) {
      this.loadComparableProducts(product.productId);
    }

    this.clearComparison();
  }

  onProduct2Selected(product: ProductDto | null): void {
    if (!product || !product.productId) {
      this.comparisonForm.patchValue({ product2: null });
      this.clearComparison();
      return;
    }

    this.comparisonForm.patchValue({ product2: product });
    this.product2SearchControl.setValue(product.name || '');
    this.clearComparison();
  }

  private loadComparableProducts(productId: string): void {
    this.comparisonService.getComparableProducts(productId, undefined, 1, 50)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.product2Options = result.items;
        },
        error: (error) => {
          console.error('Error loading comparable products:', error);
        }
      });
  }

  displayProductName(product: ProductDto | null): string {
    if (!product) return '';
    return `${product.manufacturer || ''} ${product.name || ''}`.trim();
  }

  canCompare(): boolean {
    const product1 = this.comparisonForm.get('product1')?.value;
    const product2 = this.comparisonForm.get('product2')?.value;
    return !!(product1 && product2 && product1.productId !== product2.productId);
  }

  performComparison(): void {
    if (!this.canCompare() || this.isComparing) return;

    const product1 = this.comparisonForm.get('product1')?.value!;
    const product2 = this.comparisonForm.get('product2')?.value!;
    const includePriceAnalysis = this.comparisonForm.get('includePriceAnalysis')?.value ?? true;
    const generateRecommendations = this.comparisonForm.get('generateRecommendations')?.value ?? true;

    this.isComparing = true;
    this.error = null;

    // First validate that products can be compared
    this.comparisonService.validateComparison(product1.productId!, product2.productId!)
      .pipe(
        switchMap(isValid => {
          if (!isValid) {
            throw new Error('These products cannot be compared. They must be from the same category.');
          }

          const request = new CompareProductsRequestDto({
            productId1: product1.productId!,
            productId2: product2.productId!,
            includePriceAnalysis,
            generateRecommendations
          });

          return this.comparisonService.compareProducts(request);
        }),
        takeUntil(this.destroy$)
      )
      .subscribe({
        next: (result) => {
          this.comparisonResult = result;
          this.isComparing = false;
          this.isLoading = false;

          // Update URL with product IDs
          this.router.navigate(['/product-comparison', product1.productId, product2.productId], {
            replaceUrl: true
          });
        },
        error: (error) => {
          this.error = error.message || 'Failed to compare products';
          this.isComparing = false;
          this.isLoading = false;
          this.snackBar.open(this.error || 'An error occurred', 'Close', { duration: 5000 });
        }
      });
  }

  clearComparison(): void {
    this.comparisonResult = null;
    this.error = null;

    // Reset URL if it contains product IDs
    if (this.route.snapshot.params['productId1'] || this.route.snapshot.params['productId2']) {
      this.router.navigate(['/product-comparison'], { replaceUrl: true });
    }
  }

  resetComparison(): void {
    this.comparisonForm.reset({
      includePriceAnalysis: true,
      generateRecommendations: true
    });
    this.product1SearchControl.setValue('');
    this.product2SearchControl.setValue('');
    this.product1Options = [];
    this.product2Options = [];
    this.clearComparison();
  }

  private getProductById(productId: string): Observable<ProductDto> {
    // Use the products service to get product by ID
    return this.productsService.getProducts({ search: '', pageSize: 1000 }).pipe(
      map(result => {
        const product = result.items.find(p => p.productId === productId);
        if (!product) {
          throw new Error(`Product with ID ${productId} not found`);
        }
        return product;
      })
    );
  }
}
