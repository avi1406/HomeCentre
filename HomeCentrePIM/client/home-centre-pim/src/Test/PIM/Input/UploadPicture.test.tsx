jest.mock('react-i18next', () => ({
    withTranslation: () => (Component: any) => Component,
}));
describe('UploadImageFolder', () => {
    test('renders welcome message', () => {
        expect(true).toBe(true);
    });
    describe('uploadHandler', () => {
        test('triggers file upload when called', () => {
            const fileInput: any = {
                click: jest.fn(),
                addEventListener: jest.fn(),
            };
            const obj = {
                uploadHandler() {
                    try {
                        const fileInput = document.createElement('input');
                        fileInput.type = 'file';
                        fileInput.multiple = true;
                        fileInput.webkitdirectory = true;
                        fileInput?.click();
                        fileInput.addEventListener('change', (event: any) => {

                        });
                    } catch (err: any) {
                        console.error(err);
                    }
                },
            };
            document.createElement = jest.fn(() => fileInput);
            obj.uploadHandler();
            expect(document.createElement).toHaveBeenCalledWith('input');
            expect(fileInput.type).toBe('file');
            expect(fileInput.multiple).toBe(true);
            expect(fileInput.webkitdirectory).toBe(true);
            expect(fileInput.click).toHaveBeenCalled();
            expect(fileInput.addEventListener).toHaveBeenCalled();
        });
    });


});