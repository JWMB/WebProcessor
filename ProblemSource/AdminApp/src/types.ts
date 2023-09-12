export enum SeverityLevel {
    information,
    warning,
    error,
    critical
}

export interface NotificationItem {
    createdAt: Date;
    text: string;
    details?: { [key: string]: string };
    severity?: SeverityLevel;
    data?: any;
    color?: string;
}

export interface TrainingUpdateMessage {
    TrainingId: number;
    Username: string;
    Data: any[];
    message?: string;
}
