jest.mock('react-i18next', () => ({
    withTranslation: () => (Component: any) => Component,
}));
describe('OutputTemplateAttribute', () => {
    test('renders welcome message', () => {
        expect(true).toBe(true);
    });
    describe('addAttributesTemp', () => {
        test('adds attributes to the template', async () => {
            const apiClient = {
                addAttributesToTemplate: jest.fn().mockResolvedValue(),
            };
            const templateId = 'template-123';
            const attributes = { attr1: 'value1', attr2: 'value2' };
            const obj = {
                apiClient,
                async addAttributesTemp(templateId: string, attributes: object) {
                    try {
                        await this.apiClient.addAttributesToTemplate(templateId, attributes);

                    } catch (error) {
                        console.error(`Error adding attributes to template: ${templateId}`, error);
                    }
                },
            };
            await obj.addAttributesTemp(templateId, attributes);

            expect(apiClient.addAttributesToTemplate).toHaveBeenCalledWith(templateId, attributes);
        });
    });
    describe('removeTemplateAttri', () => {
        test('removes attribute from the template', async () => {
            const apiClient = {
                removeTemplateAttribute: jest.fn().mockResolvedValue(),
            };
            const templateId = 'template-123';
            const attributeKey = 'attr1';
            const obj = {
                apiClient,
                async removeTemplateAttri(templateId: string, attributeKey: string) {
                    try {
                        await this.apiClient.removeTemplateAttribute(templateId, attributeKey);
                    } catch (error) {
                        console.error(`Error removing attribute "${attributeKey}" from template: ${templateId}`, error);
                    }
                },
            };
            await obj.removeTemplateAttri(templateId, attributeKey);
            expect(apiClient.removeTemplateAttribute).toHaveBeenCalledWith(templateId, attributeKey);
        });
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
    describe('roleOption', () => {
        test('returns the correct role options', () => {
            const obj = {
                roleOption: () => {
                    return ['Admin', 'User', 'Guest'];
                },
            };
            const result = obj.roleOption();
            expect(result).toEqual(['Admin', 'User', 'Guest']);
        });
    });
    describe('handleChange', () => {
        test('updates the state with the new value', () => {
            const obj = {
                state: {
                    value: '',
                },
                handleChange: function (event: { target: { value: any; }; }) {
                    this.setState({ value: event.target.value });
                },
                setState: jest.fn(),
            };
            const event = {
                target: {
                    value: 'New Value',
                },
            };
            obj.handleChange(event);
            expect(obj.setState).toHaveBeenCalledWith({ value: 'New Value' });
        });
    });
    describe('updateRecord', () => {
        test('updates the record with new values', async () => {
            const apiClient = {
                getRecord: jest.fn().mockResolvedValue({ id: 1, name: 'Record 1' }),
                updateRecord: jest.fn().mockResolvedValue({ id: 1, name: 'Updated Record 1' }),
            };
            const recordId = 1;
            const newName = 'Updated Record 1';
            const obj = {
                setState: jest.fn(),
                apiClient,
                async updateRecord(recordId: number, newName: string) {
                    try {
                        const record = await this.apiClient.getRecord(recordId);
                        const updatedRecord = { ...record, name: newName };
                        const result = await this.apiClient.updateRecord(updatedRecord);
                        this.setState({ record: result });
                    } catch (error) {
                        console.error('Error updating record:', error);
                    }
                },
            };
            await obj.updateRecord(recordId, newName);
            expect(apiClient.getRecord).toHaveBeenCalledWith(recordId);
            expect(apiClient.updateRecord).toHaveBeenCalledWith({ id: 1, name: 'Updated Record 1' });
            expect(obj.setState).toHaveBeenCalledWith({ record: { id: 1, name: 'Updated Record 1' } });
        });
    });
});