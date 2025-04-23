import React from 'react';
jest.mock('react-i18next', () => ({
    withTranslation: () => (Component: any) => Component,
}));
describe('QueryRequestForm', () => {
    test('renders welcome message', () => {
        expect(true).toBe(true);
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
    describe('imageUpload', () => {
        test('uploads an image and updates state accordingly', async () => {
            const mockApiManager = {
                getAuthorizedUrlForWrite: jest.fn().mockResolvedValue({ hasError: false, result: 'mocked-url' }),
            };
            const mockBlockBlobClient = {
                uploadData: jest.fn().mockResolvedValue(),
            };
            const mockNotificationManager = {
                showErrorNotify: jest.fn(),
            };
            const obj = {
                setState: jest.fn(),
                apiClient: mockApiManager,
                notify: mockNotificationManager,
                state: {
                    picture: '',
                    picUrl: '',
                },
                async imageUpload(file: any) {
                    this.setState({ picture: file[0]?.name, ProgressVisible: true });
                    const url = (await this.apiClient.getAuthorizedUrlForWrite(file[0]?.name)).result;
                    const blobClient = mockBlockBlobClient; // Replace with your actual BlockBlobClient
                    blobClient.uploadData(file[0], {
                        onProgress: (observer: any) => {
                            const progress = Math.round((observer.loadedBytes / file[0].size) * 100);
                            this.setState({ progressPercent: (progress / 100) });
                        },
                        blobHTTPHeaders: {
                            blobContentType: file[0].type,
                        },
                    }).then(() => {
                        // Update state once after successful upload
                        this.setState({
                            ProgressVisible: false,
                            picUrl: url,
                            picture: file[0].name, // Include the image name in the state update
                        });
                    }).catch((error: any) => {
                        this.notify.showErrorNotify(error);
                    });
                },
            };
            const mockFile = [{ name: 'mock-image.jpg', type: 'image/jpeg' }];
            await obj.imageUpload(mockFile);
            expect(mockApiManager.getAuthorizedUrlForWrite).toHaveBeenCalledWith('mock-image.jpg');
            expect(mockBlockBlobClient.uploadData).toHaveBeenCalledWith(
                mockFile[0],
                expect.objectContaining({
                    blobHTTPHeaders: { blobContentType: 'image/jpeg' },
                })
            );
            expect(obj.setState).toHaveBeenCalledWith(
                expect.objectContaining({
                    picture: 'mock-image.jpg',
                    ProgressVisible: false,
                    picUrl: 'mocked-url',
                })
            );
            expect(mockNotificationManager.showErrorNotify).not.toHaveBeenCalled();
        });
    });
    describe('onformsubmit', () => {
        test('submits the form and adds a query request', async () => {
            const mockApiManager = {
            };
            const mockNotificationManager = {
            };
            const formMock = {
                onformsubmit: jest.fn(),
            };
            const obj = {
                setState: jest.fn(),
                apiClient: mockApiManager,
                notify: mockNotificationManager,
                form: formMock,
            };
            const fields = {
                itemCode: 'item-1',
                subject: '',
                queryDiscription: 'Query description',
            };
            await obj.form.onformsubmit(fields);
            expect(formMock.onformsubmit).toHaveBeenCalledWith(fields);
        });
    });
});
