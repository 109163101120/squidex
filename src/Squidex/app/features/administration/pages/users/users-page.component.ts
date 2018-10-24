/*
 * Squidex Headless CMS
 *
 * @license
 * Copyright (c) Squidex UG (haftungsbeschränkt). All rights reserved.
 */

import { Component, OnInit } from '@angular/core';
import { FormControl } from '@angular/forms';
import { onErrorResumeNext } from 'rxjs/operators';

import {
    AuthService,
    Permission,
    permissionsAllow
} from '@app/shared';

import { UserDto } from './../../services/users.service';
import { UsersState } from './../../state/users.state';

const UserLockPermission =  new Permission('squidex.admin.users.lock');

@Component({
    selector: 'sqx-users-page',
    styleUrls: ['./users-page.component.scss'],
    templateUrl: './users-page.component.html'
})
export class UsersPageComponent implements OnInit {
    public usersFilter = new FormControl();

    public canLock: boolean;

    constructor(authService: AuthService,
        public readonly usersState: UsersState
    ) {
        this.canLock = permissionsAllow(authService.user!.permissions, UserLockPermission);
    }

    public ngOnInit() {
        this.usersState.load().pipe(onErrorResumeNext()).subscribe();
    }

    public reload() {
        this.usersState.load(true).pipe(onErrorResumeNext()).subscribe();
    }

    public search() {
        this.usersState.search(this.usersFilter.value).pipe(onErrorResumeNext()).subscribe();
    }

    public goPrev() {
        this.usersState.goPrev().pipe(onErrorResumeNext()).subscribe();
    }

    public goNext() {
        this.usersState.goNext().pipe(onErrorResumeNext()).subscribe();
    }

    public lock(user: UserDto) {
        this.usersState.lock(user).pipe(onErrorResumeNext()).subscribe();
    }

    public unlock(user: UserDto) {
        this.usersState.unlock(user).pipe(onErrorResumeNext()).subscribe();
    }

    public trackByUser(index: number, userInfo: { user: UserDto }) {
        return userInfo.user.id;
    }
}

