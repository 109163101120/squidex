/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */

import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import {
    HelpComponent,
    SqxFrameworkModule,
    SqxSharedModule
} from '@app/shared';

import actions from './pages/rules/actions';

const actionTypes: any[] = Object.values(actions);

import {
    AssetChangedTriggerComponent,
    ContentChangedTriggerComponent,
    RuleActionComponent,
    RuleElementComponent,
    RuleEventBadgeClassPipe,
    RuleEventsPageComponent,
    RulesPageComponent,
    RuleWizardComponent
} from './declarations';

const routes: Routes = [
    {
        path: '',
        component: RulesPageComponent,
        children: [
            {
                path: 'events',
                component: RuleEventsPageComponent
            },
            {
                path: 'help',
                component: HelpComponent,
                data: {
                    helpPage: '05-integrated/rules'
                }
            }
        ]
    }
];

@NgModule({
    imports: [
        SqxFrameworkModule,
        SqxSharedModule,
        RouterModule.forChild(routes)
    ],
    entryComponents: [
        ...actionTypes
    ],
    declarations: [
        ...actionTypes,
        AssetChangedTriggerComponent,
        ContentChangedTriggerComponent,
        RuleActionComponent,
        RuleElementComponent,
        RuleEventBadgeClassPipe,
        RuleEventsPageComponent,
        RulesPageComponent,
        RuleWizardComponent
    ]
})
export class SqxFeatureRulesModule { }