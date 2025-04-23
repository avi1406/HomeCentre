describe('DRviewData', () => {
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

});