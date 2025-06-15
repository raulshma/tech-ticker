import { Injectable, InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';

export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');

@Injectable({
  providedIn: 'root'
})
export class ApiConfigService {
  constructor() { }

  static getApiBaseUrl(): string {
    return environment.apiUrl;
  }
}
