import cacheObject from './CacheObject';


class CacheManager {

    setItem(key: string, item: any) {

        // cacheObject[key] = item;
        sessionStorage.setItem(key, JSON.stringify(item));

    }

    getItem(key: string, p: any | null) {
        // if (Object.keys(cacheObject).includes(key)) {
        if (sessionStorage.getItem(key)) {
            // return cacheObject[key];
            return JSON.parse(sessionStorage.getItem(key) || '');
        }
        if (!p) {
            return null
        }
        const r = p();
        this.setItem(key, r);
        return r;
    }

    async getItemAsync(key: string, p: Promise<any> | null) {
        if (sessionStorage.getItem(key)) {
            // return cacheObject[key];
            return JSON.parse(sessionStorage.getItem(key) || '');
        }
        if (!p) {
            return null
        }
        this.setItem(key, await p);
        return JSON.parse(sessionStorage.getItem(key) || '');
    }
}

export default CacheManager
