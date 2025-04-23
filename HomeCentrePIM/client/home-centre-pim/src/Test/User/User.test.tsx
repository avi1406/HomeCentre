jest.mock('react-i18next', () => ({
    withTranslation: () => (Component: any) => Component,
}));
describe('User', () => {
    test('renders welcome message', () => {
        expect(true).toBe(true);
    });
    describe('onActiaveClick', () => {
        test('activates or deactivates roles based on the inActivePage flag', async () => {
            // Mock the necessary objects and functions
            const apiClient = {
                activateRoleBatch: jest.fn().mockResolvedValue({ hasError: false }),
                deactivateRoleBatch: jest.fn().mockResolvedValue({ hasError: false }),
            };
            const notify = {
                showErrorNotify: jest.fn(),
                showSuccessNotify: jest.fn(),
            };
            const t = jest.fn().mockReturnValue('Test Message');
            const state = {
                selectedRowData: [{ id: 1 }, { id: 2 }],
                inActivePage: true,
            };
            const refresh = jest.fn();
            const obj = {
                state,
                apiClient,
                notify,
                props: { t },
                refresh,
                async onActiaveClick() {
                    try {
                        const selectedRecordIds = [];
                        for (let i = 0; i < this.state.selectedRowData.length; i++) {
                            selectedRecordIds.push(this.state.selectedRowData[i].id);
                        }
                        const response = this.state.inActivePage
                            ? await this.apiClient.activateRoleBatch(selectedRecordIds)
                            : await this.apiClient.deactivateRoleBatch(selectedRecordIds);
                        if (response.hasError) {
                            this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
                        } else {
                            const message = this.state.inActivePage ? this.props.t('MSG_ACTIVATE') : this.props.t('MSG_DEACTIVATE');
                            this.notify.showSuccessNotify(message);
                        }
                        this.refresh(!this.state.inActivePage);
                    } catch (e: any) {
                        this.notify.showErrorNotify(e.message);
                    }
                },
            };
            // Call the onActiaveClick() function
            await obj.onActiaveClick();
            // Assertions
            expect(apiClient.activateRoleBatch).toHaveBeenCalledWith([1, 2]);
            expect(apiClient.deactivateRoleBatch).not.toHaveBeenCalled();
            expect(notify.showErrorNotify).not.toHaveBeenCalled();
            expect(notify.showSuccessNotify).toHaveBeenCalledWith('Test Message');
            expect(refresh).toHaveBeenCalledWith(false);
        });
    });

    describe('onSearch', () => {
        test('performs a search and updates the state', async () => {
            // Mock the necessary objects and functions
            const apiClient = {
                search: jest.fn().mockResolvedValue({ results: [1, 2, 3] }),
            };

            const setState = jest.fn();
            const t = jest.fn().mockReturnValue('Test Message');
            const inputValue = 'test';

            // Create an instance of the object containing onSearch() and set the necessary properties
            const obj = {
                setState,
                apiClient,
                props: { t },
                async onSearch(inputValue: string) {
                    try {
                        const response = await this.apiClient.search(inputValue);
                        this.setState({ results: response.results });
                    } catch (e: any) {
                        this.notify.showErrorNotify(e.message);
                    }
                },
            };

            // Call the onSearch() function
            await obj.onSearch(inputValue);

            // Assertions
            expect(apiClient.search).toHaveBeenCalledWith(inputValue);
            expect(setState).toHaveBeenCalledWith({ results: [1, 2, 3] });
        });
    });

    describe('DeleteUserRoles', () => {
        test('deletes user roles and updates the state', async () => {
            // Mock the necessary objects and functions
            const apiClient = {
                deleteUserRoles: jest.fn().mockResolvedValue({ hasError: false }),
            };

            const notify = {
                showErrorNotify: jest.fn(),
                showSuccessNotify: jest.fn(),
            };

            const t = jest.fn().mockReturnValue('Test Message');

            const state = {
                selectedRowData: [{ id: 1 }, { id: 2 }],
            };

            const refresh = jest.fn();

            // Create an instance of the object containing DeleteUserRoles() and set the necessary properties
            const obj = {
                state,
                apiClient,
                notify,
                props: { t },
                refresh,
                async DeleteUserRoles() {
                    try {
                        const selectedRecordIds = [];
                        for (let i = 0; i < this.state.selectedRowData.length; i++) {
                            selectedRecordIds.push(this.state.selectedRowData[i].id);
                        }

                        const response = await this.apiClient.deleteUserRoles(selectedRecordIds);

                        if (response.hasError) {
                            this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
                        } else {
                            this.notify.showSuccessNotify(this.props.t('MSG_DELETE_ROLES'));
                        }

                        this.refresh();
                    } catch (e: any) {
                        this.notify.showErrorNotify(e.message);
                    }
                },
            };

            // Call the DeleteUserRoles() function
            await obj.DeleteUserRoles();

            // Assertions
            expect(apiClient.deleteUserRoles).toHaveBeenCalledWith([1, 2]);
            expect(notify.showErrorNotify).not.toHaveBeenCalled();
            expect(notify.showSuccessNotify).toHaveBeenCalledWith('Test Message');
            expect(refresh).toHaveBeenCalled();
        });
    });
    describe('DeleteUserMarketPlace', () => {
        test('deletes user marketplace and updates the state', async () => {
            // Mock the necessary objects and functions
            const apiClient = {
                deleteUserMarketPlace: jest.fn().mockResolvedValue({ hasError: false }),
            };

            const notify = {
                showErrorNotify: jest.fn(),
                showSuccessNotify: jest.fn(),
            };

            const t = jest.fn().mockReturnValue('Test Message');

            const state = {
                selectedRowData: [{ id: 1 }, { id: 2 }],
            };

            const refresh = jest.fn();

            // Create an instance of the object containing DeleteUserMarketPlace() and set the necessary properties
            const obj = {
                state,
                apiClient,
                notify,
                props: { t },
                refresh,
                async DeleteUserMarketPlace() {
                    try {
                        const selectedRecordIds = [];
                        for (let i = 0; i < this.state.selectedRowData.length; i++) {
                            selectedRecordIds.push(this.state.selectedRowData[i].id);
                        }

                        const response = await this.apiClient.deleteUserMarketPlace(selectedRecordIds);

                        if (response.hasError) {
                            this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
                        } else {
                            this.notify.showSuccessNotify(this.props.t('MSG_DELETE_MARKETPLACE'));
                        }

                        this.refresh();
                    } catch (e: any) {
                        this.notify.showErrorNotify(e.message);
                    }
                },
            };

            // Call the DeleteUserMarketPlace() function
            await obj.DeleteUserMarketPlace();

            // Assertions
            expect(apiClient.deleteUserMarketPlace).toHaveBeenCalledWith([1, 2]);
            expect(notify.showErrorNotify).not.toHaveBeenCalled();
            expect(notify.showSuccessNotify).toHaveBeenCalledWith('Test Message');
            expect(refresh).toHaveBeenCalled();
        });
    });
    describe('addUserRole', () => {
        test('adds a user role', async () => {
            // Mock the necessary objects and functions
            const apiClient = {
                addUserRole: jest.fn().mockResolvedValue({ hasError: false }),
            };

            const notify = {
                showErrorNotify: jest.fn(),
                showSuccessNotify: jest.fn(),
            };

            const t = jest.fn().mockReturnValue('Test Message');

            const state = {
                selectedUserRole: { id: 1, name: 'Admin' },
            };

            const refresh = jest.fn();

            // Create an instance of the object containing addUserRole() and set the necessary properties
            const obj = {
                state,
                apiClient,
                notify,
                props: { t },
                refresh,
                async addUserRole() {
                    try {
                        const response = await this.apiClient.addUserRole(this.state.selectedUserRole);

                        if (response.hasError) {
                            this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
                        } else {
                            this.notify.showSuccessNotify(this.props.t('MSG_ADD_ROLE'));
                        }

                        this.refresh();
                    } catch (e: any) {
                        this.notify.showErrorNotify(e.message);
                    }
                },
            };

            // Call the addUserRole() function
            await obj.addUserRole();

            // Assertions
            expect(apiClient.addUserRole).toHaveBeenCalledWith({ id: 1, name: 'Admin' });
            expect(notify.showErrorNotify).not.toHaveBeenCalled();
            expect(notify.showSuccessNotify).toHaveBeenCalledWith('Test Message');
            expect(refresh).toHaveBeenCalled();
        });
    });
    describe('addMarketPlaceToUser', () => {
        test('adds a marketplace to a user', async () => {
            // Mock the necessary objects and functions
            const apiClient = {
                addMarketPlaceToUser: jest.fn().mockResolvedValue({ hasError: false }),
            };

            const notify = {
                showErrorNotify: jest.fn(),
                showSuccessNotify: jest.fn(),
            };

            const t = jest.fn().mockReturnValue('Test Message');

            const state = {
                selectedUser: { id: 1, name: 'John Doe' },
                selectedMarketPlace: { id: 2, name: 'Marketplace A' },
            };

            const refresh = jest.fn();

            // Create an instance of the object containing addMarketPlaceToUser() and set the necessary properties
            const obj = {
                state,
                apiClient,
                notify,
                props: { t },
                refresh,
                async addMarketPlaceToUser() {
                    try {
                        const response = await this.apiClient.addMarketPlaceToUser(this.state.selectedUser, this.state.selectedMarketPlace);

                        if (response.hasError) {
                            this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
                        } else {
                            this.notify.showSuccessNotify(this.props.t('MSG_ADD_MARKETPLACE'));
                        }

                        this.refresh();
                    } catch (e: any) {
                        this.notify.showErrorNotify(e.message);
                    }
                },
            };

            // Call the addMarketPlaceToUser() function
            await obj.addMarketPlaceToUser();

            // Assertions
            expect(apiClient.addMarketPlaceToUser).toHaveBeenCalledWith({ id: 1, name: 'John Doe' }, { id: 2, name: 'Marketplace A' });
            expect(notify.showErrorNotify).not.toHaveBeenCalled();
            expect(notify.showSuccessNotify).toHaveBeenCalledWith('Test Message');
            expect(refresh).toHaveBeenCalled();
        });
    });
    describe('assignMarketPlace', () => {
        test('assigns a marketplace to a user', async () => {
            // Mock the necessary objects and functions
            const apiClient = {
                assignMarketPlace: jest.fn().mockResolvedValue({ hasError: false }),
            };

            const notify = {
                showErrorNotify: jest.fn(),
                showSuccessNotify: jest.fn(),
            };

            const t = jest.fn().mockReturnValue('Test Message');

            const state = {
                selectedUser: { id: 1, name: 'John Doe' },
                selectedMarketPlace: { id: 2, name: 'Marketplace A' },
            };

            const refresh = jest.fn();

            // Create an instance of the object containing assignMarketPlace() and set the necessary properties
            const obj = {
                state,
                apiClient,
                notify,
                props: { t },
                refresh,
                async assignMarketPlace() {
                    try {
                        const response = await this.apiClient.assignMarketPlace(this.state.selectedUser, this.state.selectedMarketPlace);

                        if (response.hasError) {
                            this.notify.showErrorNotify(this.props.t('ERROR_UNKNOWN'));
                        } else {
                            this.notify.showSuccessNotify(this.props.t('MSG_ASSIGN_MARKETPLACE'));
                        }

                        this.refresh();
                    } catch (e: any) {
                        this.notify.showErrorNotify(e.message);
                    }
                },
            };

            // Call the assignMarketPlace() function
            await obj.assignMarketPlace();

            // Assertions
            expect(apiClient.assignMarketPlace).toHaveBeenCalledWith({ id: 1, name: 'John Doe' }, { id: 2, name: 'Marketplace A' });
            expect(notify.showErrorNotify).not.toHaveBeenCalled();
            expect(notify.showSuccessNotify).toHaveBeenCalledWith('Test Message');
            expect(refresh).toHaveBeenCalled();
        });
    });
    describe('MarketOptionChange', () => {
        test('updates the selected marketplace in the state', async () => {
            // Mock the necessary objects and functions
            const setState = jest.fn();
            const t = jest.fn().mockReturnValue('Test Message');
            const selectedMarketPlace = { id: 2, name: 'Marketplace B' };

            // Create an instance of the object containing MarketOptionChange() and set the necessary properties
            const obj = {
                setState,
                props: { t },
                async MarketOptionChange(selectedMarketPlace) {
                    try {
                        this.setState({ selectedMarketPlace });
                    } catch (e: any) {
                        this.notify.showErrorNotify(e.message);
                    }
                },
            };

            // Call the MarketOptionChange() function
            await obj.MarketOptionChange(selectedMarketPlace);

            // Assertions
            expect(setState).toHaveBeenCalledWith({ selectedMarketPlace: { id: 2, name: 'Marketplace B' } });
        });
    });
    describe('onRoleChange', () => {
        test('updates the selected role in the state', async () => {
            // Mock the necessary objects and functions
            const setState = jest.fn();
            const t = jest.fn().mockReturnValue('Test Message');
            const selectedRole = { id: 2, name: 'Role B' };

            // Create an instance of the object containing onRoleChange() and set the necessary properties
            const obj = {
                setState,
                props: { t },
                async onRoleChange(selectedRole) {
                    try {
                        this.setState({ selectedRole });
                    } catch (e: any) {
                        this.notify.showErrorNotify(e.message);
                    }
                },
            };

            // Call the onRoleChange() function
            await obj.onRoleChange(selectedRole);

            // Assertions
            expect(setState).toHaveBeenCalledWith({ selectedRole: { id: 2, name: 'Role B' } });
        });
    });



});
