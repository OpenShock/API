import axios from 'axios';
import storeF from '@/store'
import router from '@/router'

const state = storeF.getters;

class ApiCall {

	async makeCall(method, path, data) {
		try {
			return await axios({
				method: method,
				url: config.apiUrl + path,
				data: data,
				headers: {
					'Auth-Key': state.getAuthKey()
				}
			});
		} catch (err) {
			toastr.error(err);
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
