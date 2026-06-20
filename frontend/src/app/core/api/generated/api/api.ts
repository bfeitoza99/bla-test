export * from './auth.service';
import { AuthService } from './auth.service';
export * from './auth.serviceInterface';
export * from './tasks.service';
import { TasksService } from './tasks.service';
export * from './tasks.serviceInterface';
export const APIS = [AuthService, TasksService];
