import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BrowserAutomationTestHubService {
  private hubConnection: HubConnection | null = null;
  private isConnected = false;

  // Observables for real-time updates
  private _browserStateUpdate$ = new Subject<any>();
  private _logEntry$ = new Subject<any>();
  private _performanceMetrics$ = new Subject<any>();
  private _testCompleted$ = new Subject<any>();
  private _error$ = new Subject<any>();
  private _connectionState$ = new BehaviorSubject<string>('Disconnected');

  // Public observables
  readonly browserStateUpdate$ = this._browserStateUpdate$.asObservable();
  readonly logEntry$ = this._logEntry$.asObservable();
  readonly performanceMetrics$ = this._performanceMetrics$.asObservable();
  readonly testCompleted$ = this._testCompleted$.asObservable();
  readonly error$ = this._error$.asObservable();
  readonly connectionState$ = this._connectionState$.asObservable();

  constructor() {
    this.buildConnection();
  }

  private buildConnection(): void {
    const token = localStorage.getItem('token');
    
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`${environment.apiUrl}/hubs/browser-automation-test`, {
        accessTokenFactory: () => token || ''
      })
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .configureLogging(LogLevel.Information)
      .build();

    this.setupEventHandlers();
  }

  private setupEventHandlers(): void {
    if (!this.hubConnection) return;

    // Connection state events
    this.hubConnection.onclose(() => {
      this.isConnected = false;
      this._connectionState$.next('Disconnected');
      console.log('SignalR connection closed');
    });

    this.hubConnection.onreconnecting(() => {
      this._connectionState$.next('Reconnecting');
      console.log('SignalR reconnecting...');
    });

    this.hubConnection.onreconnected(() => {
      this.isConnected = true;
      this._connectionState$.next('Connected');
      console.log('SignalR reconnected');
    });

    // Browser automation test events
    this.hubConnection.on('BrowserStateUpdate', (message: any) => {
      console.log('Browser state update received:', message);
      this._browserStateUpdate$.next(message.data);
    });

    this.hubConnection.on('LogEntry', (message: any) => {
      console.log('Log entry received:', message);
      this._logEntry$.next(message.data);
    });

    this.hubConnection.on('PerformanceMetrics', (message: any) => {
      console.log('Performance metrics received:', message);
      this._performanceMetrics$.next(message.data);
    });

    this.hubConnection.on('TestCompleted', (message: any) => {
      console.log('Test completed:', message);
      this._testCompleted$.next(message.data);
    });

    this.hubConnection.on('Error', (message: any) => {
      console.error('Test error received:', message);
      this._error$.next(message.data);
    });
  }

  async connect(): Promise<void> {
    if (!this.hubConnection) {
      this.buildConnection();
    }

    if (this.hubConnection && !this.isConnected) {
      try {
        this._connectionState$.next('Connecting');
        await this.hubConnection.start();
        this.isConnected = true;
        this._connectionState$.next('Connected');
        console.log('SignalR connection established');
      } catch (error) {
        this._connectionState$.next('Error');
        console.error('Error connecting to SignalR hub:', error);
        throw error;
      }
    }
  }

  async disconnect(): Promise<void> {
    if (this.hubConnection && this.isConnected) {
      try {
        await this.hubConnection.stop();
        this.isConnected = false;
        this._connectionState$.next('Disconnected');
        console.log('SignalR connection stopped');
      } catch (error) {
        console.error('Error disconnecting from SignalR hub:', error);
      }
    }
  }

  async joinTestSession(sessionId: string): Promise<void> {
    if (!this.hubConnection || !this.isConnected) {
      throw new Error('SignalR connection not established');
    }

    try {
      await this.hubConnection.invoke('JoinTestSession', sessionId);
      console.log(`Joined test session: ${sessionId}`);
    } catch (error) {
      console.error('Error joining test session:', error);
      throw error;
    }
  }

  async leaveTestSession(sessionId: string): Promise<void> {
    if (!this.hubConnection || !this.isConnected) {
      return;
    }

    try {
      await this.hubConnection.invoke('LeaveTestSession', sessionId);
      console.log(`Left test session: ${sessionId}`);
    } catch (error) {
      console.error('Error leaving test session:', error);
    }
  }

  getConnectionState(): string {
    return this._connectionState$.value;
  }

  isConnectionEstablished(): boolean {
    return this.isConnected;
  }
} 