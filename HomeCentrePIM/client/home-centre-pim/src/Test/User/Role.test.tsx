jest.mock('react-i18next', () => ({
    withTranslation: () => (Component: any) => Component,
}));
describe('onActiaveClick', () => {
    test('activates or deactivates roles based on the inActivePage flag', async () => {
        // Mock the necessary objects and functions
        const apiClient = {
            activateRoleBatch: jest.fn().mockResolvedValue({ hasError: false }),
            deactivateRoleBatch: jest.fn().mockResolvedValue({ hasError: false })
        };
        const notify = {
            showErrorNotify: jest.fn(),
            showSuccessNotify: jest.fn()
        };
        const t = jest.fn().mockReturnValue('Test Message');
        const state = {
            selectedRowData: [{ id: 1 }, { id: 2 }],
            inActivePage: true
        };
        const refresh = jest.fn();
        // Create an instance of the object containing onActiaveClick() and set the necessary properties
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
            }
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
            }
        };
        // Call the onSearch() function
        await obj.onSearch(inputValue);
        // Assertions
        expect(apiClient.search).toHaveBeenCalledWith(inputValue);
        expect(setState).toHaveBeenCalledWith({ results: [1, 2, 3] });
    });
});
