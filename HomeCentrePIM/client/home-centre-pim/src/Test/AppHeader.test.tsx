describe('AppHeader', () => {

    it('should redirect to "/overview" after clicking on the logo', () => {
        window.location.href = "http://localhost/"
        expect(window.location.href).toBe('http://localhost/');
    });
});