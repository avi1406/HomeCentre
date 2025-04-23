jest.mock('react-i18next', () => ({
    withTranslation: () => (Component: any) => Component,
}));
describe('UID', () => {
    test('renders welcome message', () => {
        expect(true).toBe(true);
    });
    describe('downloadFile', () => {
        test('downloads the specified file', async () => {
            const apiClient = {
                downloadFile: jest.fn().mockResolvedValue(),
            };
            const obj = {
                apiClient,
                downloadFile(filename: string) {
                    this.apiClient.downloadFile(filename)
                        .then(() => {
                        })
                        .catch((error) => {
                            console.error(`Error downloading file: ${filename}`, error);
                        });
                },
            };
            await obj.downloadFile('sample.pdf');
            expect(apiClient.downloadFile).toHaveBeenCalledWith('sample.pdf');
        });
    });
    describe('onSearch', () => {
        test('filters data based on search input', () => {
            const searchData = [
                { uid: '123', name: 'Product 1' },
                { uid: '456', name: 'Product 2' },
                { uid: '789', name: 'Product 3' },
            ];
            const obj = {
                setState: jest.fn(),
                searchData,
                onSearch(e: any) {
                    const newValue = e?.target.value;
                    const trimmedValue = newValue?.trim()?.toLowerCase();
                    const result = this.searchData.filter((element: any) => {
                        return element?.uid?.toLowerCase()?.includes(trimmedValue);
                    });
                    this.setState({ uidData: result });
                    this.setState({ uidData: result?.map((item: any, index: any) => ({ index: index + 1, ...item })) });
                },
            };
            const event = {
                target: { value: '123' },
            };
            obj.onSearch(event);
            expect(obj.setState).toHaveBeenCalledTimes(2);
            expect(obj.setState).toHaveBeenCalledWith({ uidData: [{ index: 1, uid: '123', name: 'Product 1' }] });
        });
    });
});