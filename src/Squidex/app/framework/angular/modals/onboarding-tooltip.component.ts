/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */

import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnDestroy, OnInit, Renderer2 } from '@angular/core';

import {
    fadeAnimation,
    ModalModel,
    OnboardingService,
    Types
} from '@app/framework/internal';

@Component({
    selector: 'sqx-onboarding-tooltip',
    styleUrls: ['./onboarding-tooltip.component.scss'],
    templateUrl: './onboarding-tooltip.component.html',
    animations: [
        fadeAnimation
    ],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class OnboardingTooltipComponent implements OnDestroy, OnInit {
    private showTimer: any;
    private closeTimer: any;
    private forMouseDownListener: Function | null;

    public tooltipModal = new ModalModel();

    @Input()
    public for: any;

    @Input()
    public helpId: string;

    @Input()
    public after = 1000;

    @Input()
    public position = 'left';

    constructor(
        private readonly changeDetector: ChangeDetectorRef,
        private readonly onboardingService: OnboardingService,
        private readonly renderer: Renderer2
    ) {
    }

    public ngOnDestroy() {
        clearTimeout(this.showTimer);
        clearTimeout(this.closeTimer);

        this.tooltipModal.hide();

        if (this.forMouseDownListener) {
            this.forMouseDownListener();
            this.forMouseDownListener = null;
        }
    }

    public ngOnInit() {
        if (this.for && this.helpId && Types.isFunction(this.for.addEventListener)) {
            this.showTimer = setTimeout(() => {
                if (this.onboardingService.shouldShow(this.helpId)) {
                    const forRect = this.for.getBoundingClientRect();

                    const x = forRect.left + 0.5 * forRect.width;
                    const y = forRect.top  + 0.5 * forRect.height;

                    const fromPoint = document.elementFromPoint(x, y);

                    if (this.isSameOrParent(fromPoint)) {
                        this.tooltipModal.show();

                        this.changeDetector.markForCheck();

                        this.closeTimer = setTimeout(() => {
                            this.hideThis();
                        }, 10000);

                        this.onboardingService.disable(this.helpId);
                    }
                }
            }, this.after);

            this.forMouseDownListener =
                this.renderer.listen(this.for, 'mousedown', () => {
                    this.onboardingService.disable(this.helpId);

                    this.hideThis();
                });
        }
    }

    private isSameOrParent(underCursor: Element | null): boolean {
        if (!underCursor) {
            return false;
        } if (this.for === underCursor) {
            return true;
        } else {
            return this.isSameOrParent(this.renderer.parentNode(underCursor));
        }
    }

    public hideThis() {
        this.onboardingService.disable(this.helpId);

        this.ngOnDestroy();
    }

    public hideAll() {
        this.onboardingService.disableAll();

        this.ngOnDestroy();
    }
}