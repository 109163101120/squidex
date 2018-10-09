/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Sebastian Stehle. All rights r vbeserved
 */

import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';

import {
    DialogModel,
    DialogRequest,
    DialogService,
    fadeAnimation,
    Notification
} from '@app/framework/internal';

@Component({
    selector: 'sqx-dialog-renderer',
    styleUrls: ['./dialog-renderer.component.scss'],
    templateUrl: './dialog-renderer.component.html',
    animations: [
        fadeAnimation
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class DialogRendererComponent implements OnDestroy, OnInit {
    private dialogSubscription: Subscription;
    private dialogsSubscription: Subscription;
    private notificationsSubscription: Subscription;

    public dialogView = new DialogModel();
    public dialogRequest: DialogRequest | null = null;

    public notifications: Notification[] = [];

    @Input()
    public position = 'bottomright';

    constructor(
        private readonly changeDetector: ChangeDetectorRef,
        private readonly dialogs: DialogService
    ) {
    }

    public ngOnDestroy() {
        this.notificationsSubscription.unsubscribe();
        this.dialogSubscription.unsubscribe();
        this.dialogsSubscription.unsubscribe();
    }

    public ngOnInit() {
        this.dialogSubscription =
            this.dialogView.isOpen.subscribe(isOpen => {
                if (!isOpen) {
                    this.cancel();

                    this.changeDetector.detectChanges();
                }
            });

        this.notificationsSubscription =
            this.dialogs.notifications.subscribe(notification => {
                this.notifications.push(notification);

                if (notification.displayTime > 0) {
                    setTimeout(() => {
                        this.close(notification);
                    }, notification.displayTime);
                }

                this.changeDetector.detectChanges();
            });

        this.dialogsSubscription =
            this.dialogs.dialogs
                .subscribe(request => {
                    this.cancel();

                    this.dialogRequest = request;
                    this.dialogView.show();

                    this.changeDetector.detectChanges();
                });
    }

    public cancel() {
        if (this.dialogRequest) {
            this.dialogRequest.complete(false);
            this.dialogRequest = null;
            this.dialogView.hide();
        }
    }

    public confirm() {
        if (this.dialogRequest) {
            this.dialogRequest.complete(true);
            this.dialogRequest = null;
            this.dialogView.hide();
        }
    }

    public close(notification: Notification) {
        const index = this.notifications.indexOf(notification);

        if (index >= 0) {
            this.notifications.splice(index, 1);

            this.changeDetector.detectChanges();
        }
    }
}