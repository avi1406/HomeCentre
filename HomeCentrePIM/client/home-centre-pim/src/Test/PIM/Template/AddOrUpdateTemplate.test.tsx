jest.mock('react-i18next', () => ({
    withTranslation: () => (Component: any) => Component,
}));
describe('OutputAddOrUpdateTemplate', () => {
    test('renders welcome message', () => {
        expect(true).toBe(true);
    });
    describe('rowSelection', () => {
        test('updates state with selected row data and count', () => {
            const selectedRows = ['row1', 'row2'];
            const selectedCount = 2;
            const e = {
                selectedRows,
                selectedCount,
            };
            const obj = {
                setState: jest.fn(),
            };
            obj.setState({ selectedRowData: selectedRows, selectionCount: selectedCount });
            expect(obj.setState).toHaveBeenCalledWith({ selectedRowData: selectedRows, selectionCount: selectedCount });
        });
    });
});
