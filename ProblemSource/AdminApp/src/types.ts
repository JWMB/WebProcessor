export enum SeverityLevel {
    information,
    warning,
    error,
    critical
}

export interface NotificationItem {
    createdAt: Date;
    text: string;
    severity?: SeverityLevel;
}