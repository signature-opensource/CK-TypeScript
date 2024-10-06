import { AxiosInstance, AxiosHeaders, RawAxiosRequestConfig } from "axios";

const defaultCrisAxiosConfig: RawAxiosRequestConfig = {
    responseType: 'text',
    headers: {
        common: new AxiosHeaders({
            'Content-Type': 'application/json'
        })
    }
};

export class HttpCrisEndpoint {

    #axios: AxiosInstance;
    #axiosConfig: RawAxiosRequestConfig;

    constructor(axios: AxiosInstance) {
        this.#axios = axios;
        this.#axiosConfig = defaultCrisAxiosConfig;
    }

    public async sendAsync<T>() {
        await this.#axios.post('', null, this.#axiosConfig);
    }
}
