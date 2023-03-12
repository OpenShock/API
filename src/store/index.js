import { createStore } from 'vuex';


const store = createStore({
	state () {
		return {
			user: {
				id: "",
				name: "",
				image: ""
			},
			secondLevelNav: [],
			settings: {
				dark: true
			},
			devicesUpdatedCounter: 0,
			deviceStates: []
		}
	},
	getters: {
		getAuthKey: (state) => () => state.authKey
	},
	mutations: {
		setUser (state, user) {
			state.user = user;
		},
		setAuthKey(state, authKey) {
			state.authKey = authKey;
		},
		setSNav(state, nav) {
			state.secondLevelNav = nav;
		},
		setDarkMode(state, dark) {
			state.settings.dark = dark;
		},
		setDeviceState(state, {id, online}) {
			state.devicesUpdatedCounter++;
			state.deviceStates[id] = online;
		}
	},
	actions: {
		setNewNav({commit, state}, nav) {
			const timeout = state.secondLevelNav.length > 0;
			commit('setSNav', []);
			if(timeout) {
				setTimeout(() => {
					commit('setSNav', nav);
				}, 200);
			} else {
				commit('setSNav', nav);
			}
		},
		setDarkMode({commit}, dark) {
			commit('setDarkMode', dark);
			utils.setDarkMode(dark);
		},
		setDeviceState({commit}, {id, online}) {
			commit('setDeviceState', {id, online});
		},
		async getSelf({commit}) {
			const res = await apiCall.makeCall('GET', '1/users/self');
			if (res === undefined || res.status !== 200) {
				toastr.error("Error while retrieving user information");
				return;
			}

			const data = res.data.data;
			commit('setUser', {
				id: data.id,
				name: data.name,
				image: data.image
			});
		}
	},
	modules: {

	}
})

export default store
