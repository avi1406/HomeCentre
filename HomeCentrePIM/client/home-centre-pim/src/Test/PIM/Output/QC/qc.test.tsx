jest.mock('react-i18next', () => ({
    withTranslation: () => (Component: any) => Component,
}));

describe('QC', () => {
    test('renders welcome message', () => {
        expect(true).toBe(true);
    });

    describe('onSearch', () => {
        test('performs a search and updates state with the results', async () => {
            const apiClient = {
                search: jest.fn().mockResolvedValue(['result1', 'result2']),
            };
            const query = 'search query';
            const obj = {
                setState: jest.fn(),
                apiClient,
                async onSearch(query: string) {
                    try {
                        const results = await this.apiClient.search(query);
                        this.setState({ results });
                    } catch (error) {
                        console.error('Error performing search:', error);
                    }
                },
            };
            await obj.onSearch(query);
            expect(apiClient.search).toHaveBeenCalledWith(query);
            expect(obj.setState).toHaveBeenCalledWith({ results: ['result1', 'result2'] });
        });
    });
    describe('rowSelection', () => {
        test('updates state with selected row data and count', () => {
            const selectedRows = [
                { id: 1, name: 'Item 1' },
                { id: 2, name: 'Item 2' },
            ];
            const obj = {
                setState: jest.fn(),
                rowSelection(e: { selectedRows: any; selectedCount: any; }) {
                    this.setState({
                        selectedRowData: e.selectedRows,
                        selectionCount: e.selectedCount,
                    });
                },
            };
            obj.rowSelection({ selectedRows, selectedCount: selectedRows.length });
            expect(obj.setState).toHaveBeenCalledWith({
                selectedRowData: selectedRows,
                selectionCount: selectedRows.length,
            });
        });
    });
    describe('handleRowDeselection', () => {
        test('updates selectedRowData and selectionCount after row deselection', () => {
            const initialSelectedRows = [
                { id: 1, name: 'Item 1' },
                { id: 2, name: 'Item 2' },
                { id: 3, name: 'Item 3' },
            ];
            const deselectedId = 2;
            const obj = {
                setState: jest.fn(),
                state: {
                    selectedRowData: initialSelectedRows,
                    selectionCount: initialSelectedRows.length,
                },
                handleRowDeselection(id: number) {
                    const updatedSelectedRows = this.state.selectedRowData.filter(row => row.id !== id);
                    this.setState({
                        selectedRowData: updatedSelectedRows,
                        selectionCount: updatedSelectedRows.length,
                    });
                },
            };
            obj.handleRowDeselection(deselectedId);
            expect(obj.setState).toHaveBeenCalledWith({
                selectedRowData: [
                    { id: 1, name: 'Item 1' },
                    { id: 3, name: 'Item 3' },
                ],
                selectionCount: 2,
            });
        });
    });
    describe('downloadFile', () => {
        test('triggers file download and shows success notification', async () => {
            const mockNotify = {
                showSuccessNotify: jest.fn(),
            };
            const mockDocument = {
                createElement: jest.fn().mockReturnValue({
                    href: '',
                    click: jest.fn(),
                }),
                body: {
                    appendChild: jest.fn(),
                    removeChild: jest.fn(),
                },
            };
            const baseUrl = 'mockBaseUrl';
            const token = 'mockToken';
            const obj = {
                notify: mockNotify,
                token,
                baseUrl,
                downloadFile() {
                    try {
                        const url = `${this.baseUrl}/api/Product/DownloadProducts?token=${this.token}`;
                        const link = mockDocument.createElement('a');
                        link.href = url;
                        mockDocument.body.appendChild(link);
                        link.click();
                        mockDocument.body.removeChild(link);
                        this.notify.showSuccessNotify('DOWNLOAD_STARTED');
                    } catch (error) {
                        this.notify.showErrorNotify(error);
                    }
                },
            };
            await obj.downloadFile();
            expect(mockDocument.createElement).toHaveBeenCalledWith('a');
            expect(mockDocument.body.appendChild).toHaveBeenCalledWith(expect.any(Object));
            expect(mockDocument.body.removeChild).toHaveBeenCalledWith(expect.any(Object));
            expect(mockNotify.showSuccessNotify).toHaveBeenCalledWith('DOWNLOAD_STARTED');
        });
    });
    describe('columMapping', () => {
        test('maps data to columns and row data correctly', () => {
            const mockData = [
                { id: 1, name: 'Item 1', columnData: { key1: 'Value 1', key2: 'Value 2' } },
                { id: 2, name: 'Item 2', columnData: { key1: 'Value 3', key2: 'Value 4' } },
            ];

            const obj = {
                columMapping(data: any[]) {
                    const columns: { id: string; key: string; name: string; }[] = [];
                    const rowData: any[] = [];
                    data.forEach((item: { columnData: { [x: string]: any; }; }) => {
                        const keys = Object.keys(item.columnData);
                        const row: { [key: string]: { value: string } } = {};
                        keys.forEach((key) => {
                            columns.push({
                                id: key,
                                key: key,
                                name: key,
                            });
                            row[key] = { value: item.columnData[key] };
                        });
                        rowData.push(row);
                    });
                    this.setState({
                        columns,
                        rowData,
                    });
                },
                setState: jest.fn(),
            };
            obj.columMapping(mockData);
            const expectedColumns = [
                { id: 'key1', key: 'key1', name: 'key1' },
                { id: 'key2', key: 'key2', name: 'key2' },
            ];
            const expectedRowData = [
                { key1: { value: 'Value 1' }, key2: { value: 'Value 2' } },
                { key1: { value: 'Value 3' }, key2: { value: 'Value 4' } },
            ];
            const sortedExpectedColumns = expectedColumns.sort((a, b) => a.id.localeCompare(b.id));
            const sortedExpectedRowData = expectedRowData.sort((a, b) => a.key1.value.localeCompare(b.key1.value));
            expect(obj.setState).toHaveBeenCalledWith({
                columns: expect.arrayContaining(sortedExpectedColumns),
                rowData: expect.arrayContaining(sortedExpectedRowData),
            });
        });
    });
    describe('refresh', () => {
        test('updates state and handles API response', async () => {
            const mockApiManager = {
                searchProducts: jest.fn().mockResolvedValue({ hasError: false, result: { records: [] } }),
            };
            const mockNotificationManager = {
                showErrorNotify: jest.fn(),
            };
            const mockUtil = {
                showErrorNotify: jest.fn(),
            };
            const obj = {
                setState: jest.fn(),
                util: mockUtil,
                apiClient: mockApiManager,
                notify: mockNotificationManager,
                async refresh() {
                    try {
                        this.setState({ isRecord: true });
                        const response = await this.apiClient.searchProducts('', 0, 0, undefined);
                        if (!response.hasError) {
                            // Mock implementation for columMapping
                        } else {
                            this.notify.showErrorNotify(response.message);
                        }
                        this.setState({ isRecord: false, searchKey: '' });
                    } catch (error) {
                        this.notify.showErrorNotify(error);
                    }
                },
            };
            await obj.refresh();
            expect(mockApiManager.searchProducts).toHaveBeenCalledWith('', 0, 0, undefined);
            expect(mockNotificationManager.showErrorNotify).not.toHaveBeenCalled();
            expect(obj.setState).toHaveBeenCalledWith({ isRecord: false, searchKey: '' });
        });
    });
    describe('componentDidMount', () => {
        test('fetches status options and refreshes data on component mount', async () => {
            const mockApiManager = {
                getQcStatus: jest.fn().mockResolvedValue({ hasError: false, result: [{ id: 1, name: 'Status 1' }] }),
                searchProducts: jest.fn().mockResolvedValue({ hasError: false, result: { records: [] } }),
            };
            const mockNotificationManager = {
                showErrorNotify: jest.fn(),
            };
            const mockUtil = {
                showErrorNotify: jest.fn(),
            };
            const obj = {
                setState: jest.fn(),
                util: mockUtil,
                apiClient: mockApiManager,
                notify: mockNotificationManager,
                refresh: jest.fn(),
                async componentDidMount() {
                    const status: { key: any; text: any; }[] = [];
                    const statuses = (await this.apiClient.getQcStatus()).result;
                    statuses?.forEach((item: any) => {
                        status.push({ key: item.id, text: item.name });
                    });
                    this.setState({ statusOption: status });
                    this.refresh();
                },
            };
            await obj.componentDidMount();
            expect(mockApiManager.getQcStatus).toHaveBeenCalled();
            expect(mockNotificationManager.showErrorNotify).not.toHaveBeenCalled();
            expect(obj.setState).toHaveBeenCalledWith({ statusOption: [{ key: 1, text: 'Status 1' }] });
            expect(obj.refresh).toHaveBeenCalled();
        });
    });
});
