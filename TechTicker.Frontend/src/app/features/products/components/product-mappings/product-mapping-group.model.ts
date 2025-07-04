import { ProductSellerMappingDto } from "../../../../shared/api/api-client";

export interface ProductMappingGroup {
  productName: string;
  mappings: ProductSellerMappingDto[];
}
