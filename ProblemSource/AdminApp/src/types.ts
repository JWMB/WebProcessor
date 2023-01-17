export enum SeverityLevel {
    information,
    warning,
    error,
    critical
}

export interface NotificationItem {
    createdAt: Date;
    text: string;
    details?: {[key: string]: string};
    severity?: SeverityLevel;
    data?: any;
}

export interface TrainingUpdateMessage {
    trainingId: number;
    username: string;
    events: any[];
    message?: string;
}
