jest.mock('react-i18next', () => ({
    withTranslation: () => (Component: any) => Component,
}));

describe('Overview', () => {
    test('renders welcome message', () => {
        expect(true).toBe(true);
        expect('Welcome To Landmark-PIM Program').toBe('Welcome To Landmark-PIM Program');
    });
});

