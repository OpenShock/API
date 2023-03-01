import {createRouter, createWebHashHistory} from 'vue-router'
import storeF from '@/store'
const state = storeF.getters;

function lazyLoad(view){
	return() => import(`@/views/${view}`)
}

const routes = [
	{
		path: '/',
		component: lazyLoad('Root'),
	},
	{
		path: '/manager',
		component: lazyLoad('manager/ManagerRoot'),
		redirect: '/manager/dashboard',
		children: [
			{
				path: 'dashboard',
				component: lazyLoad('manager/dashboard/DashboardRoot')
			},
			{
				path: 'admin',
				component: lazyLoad('manager/admin/AdminRoot'),
				redirect: '/manager/admin/users',
				children: [
					{
						path: 'users',
						component: lazyLoad('manager/admin/users/Users')
					},
					{
						path: 'redeemables',
						component: lazyLoad('manager/admin/redeemables/Redeemables')
					},
					{
						path: 'sdk-versions',
						component: lazyLoad('manager/admin/sdk-versions/SdkVersions')
					}
				]
			},
			{
				path: 'profile',
				component: lazyLoad('manager/profile/ProfileRoot'),
				redirect: '/manager/profile/account',
				children: [
					{
						path: 'account',
						component: lazyLoad('manager/profile/Account')
					},
					{
						path: 'settings',
						component: lazyLoad('manager/profile/Settings')
					},
					{
						path: 'license',
						component: lazyLoad('manager/profile/License')
					},
					{
						path: "connections",
						component: lazyLoad('manager/profile/connections/ConnectionsRoot'),
						redirect: '/manager/profile/settings',
						children: [
							{
								path: "patreon",
								component: lazyLoad('manager/profile/connections/Patreon')
							}
						]
					}
				]
			}
		]
	},
	{
		path: '/account',
		component: lazyLoad('Login/AppRoot'),
		redirect: '/account/login',
		children: [
			{
				path: 'login',
				component: lazyLoad('Login/Login')
			},
			{
				path: 'signup',
				component: lazyLoad('Login/Signup')
			},
			{
				path: 'password',
				component: lazyLoad('Login/Password/Root'),
				redirect: '/account/password/reset',
				children: [
					{
						path: 'reset',
						component: lazyLoad('Login/Password/Reset')
					},
					{
						path: 'recover/:uuid/:secret',
						component: lazyLoad('Login/Password/Recover'),
						props: true
					}
				]
			},
			{
				path: 'activate/:uuid/:secret',
				component: lazyLoad('Login/Activate'),
				props: true
			}
		]
	}
]

const router = createRouter({
	history: createWebHashHistory(),
	routes
})

router.beforeEach((to, from, next) => {
	emitter.emit('route-before');
	next();
})

router.afterEach((to, from) => {
	emitter.emit('route-after');
})

export default router
