/*
 * Squidex Headless CMS
 * 
 * @license
 * Copyright (c) Sebastian Stehle. All rights reserved
 */

import { Component, Input, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Observable } from 'rxjs';

import { fadeAnimation } from 'shared';

@Component({
    selector: 'sqx-string-ui',
    styleUrls: ['string-ui.component.scss'],
    templateUrl: 'string-ui.component.html',
    animations: [
        fadeAnimation
    ]
})
export class StringUIComponent implements OnInit {
    @Input()
    public editForm: FormGroup;

    public hideAllowedValues: Observable<boolean>;

    public ngOnInit() {
        this.editForm.addControl('editor',
            new FormControl('Input', [
                Validators.required
            ]));
        this.editForm.addControl('placeholder',
            new FormControl('', [
                Validators.maxLength(100)
            ]));
        this.editForm.addControl('allowedValues',
            new FormControl(10, []));

        this.hideAllowedValues =
            Observable.of(false)
                .merge(this.editForm.get('editor').valueChanges)
                .map(x => !x || x === 'Input' || x === 'Textarea');
    }
}