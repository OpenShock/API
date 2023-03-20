import axios from 'axios';
import storeF from '@/store'
import router from '@/router'

axios.defaults.withCredentials = true
const state = storeF.getters;

class ApiCall {

	async makeCall(method, path, data) {
		try {
			return await axios({
				method: method,
				url: config.apiUrl + path,
				data: data
			});
		} catch (err) {
			toastr.error(utils.getError(err), "API interaction failed");
			if(err.response !== undefined && err.response.status === 401) {
				router.push('/account/login');
				utils.setLogin("");
				return undefined;
			}

			throw err;
		}
	}

	async makeCall(method, path, data, headers) {
		try {
			return await axios({
				method: method,
				url: config.apiUrl + path,
				data: data,
				headers: headers
			});
		} catch (err) {
			toastr.error(utils.getError(err), "API interaction failed");
			if(err.response !== undefined && err.response.status === 401) {
				router.push('/account/login');
				utils.setLogin("");
				return undefined;
			}

			throw err;
		}
	}
}

export default new ApiCall();
