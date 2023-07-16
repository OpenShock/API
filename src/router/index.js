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
		path: '/proxy/shares/links/:id',
		component: lazyLoad('proxy/ShareLinksProxy'),
		props: true
	},
	{
		path: '/dashboard',
		component: lazyLoad('dashboard/DashboardRoot'),
		redirect: '/dashboard/home',
		children: [
			{
				path: 'home',
				component: lazyLoad('dashboard/dashboard/DashboardRoot')
			},
			{
				path: 'shockers',
				component: lazyLoad('dashboard/shockers/ShockersRoot'),
				redirect: '/dashboard/shockers/own',
				children: [
					{
						path: 'own',
						component: lazyLoad('dashboard/shockers/own/Own')
					},
					{
						path: 'shared',
						component: lazyLoad('dashboard/shockers/shared/Shared')
					},
					{
						path: ':id/shares',
						component: lazyLoad('dashboard/shockers/own/shares/ShockerSharesRoot')
					},
					{
						path: ':id/logs',
						component: lazyLoad('dashboard/shockers/own/ShockerLogs')
					},
				]
			},
			{
				path: 'shares',
				component: lazyLoad('dashboard/shares/SharesRoot'),
				redirect: '/dashboard/shares/links',
				children: [
					{
						path: 'links',
						component: lazyLoad('dashboard/shares/links/ShareLinksRoot')
					},
					{
						path: 'links/:id',
						component: lazyLoad('dashboard/shares/links/ViewShareLink'),
						props: true
					}
				]
			},
			{
				path: 'admin',
				component: lazyLoad('dashboard/admin/AdminRoot'),
				redirect: '/dashboard/admin/users',
				children: [
					{
						path: 'users',
						component: lazyLoad('dashboard/admin/users/Users')
					},
					{
						path: 'redeemables',
						component: lazyLoad('dashboard/admin/redeemables/Redeemables')
					},
					{
						path: 'sdk-versions',
						component: lazyLoad('dashboard/admin/sdk-versions/SdkVersions')
					}
				]
			},
			{
				path: 'profile',
				component: lazyLoad('dashboard/profile/ProfileRoot'),
				redirect: '/dashboard/profile/account',
				children: [
					{
						path: 'account',
						component: lazyLoad('dashboard/profile/Account')
					},
					{
						path: 'settings',
						component: lazyLoad('dashboard/profile/Settings')
					},
					{
						path: 'license',
						component: lazyLoad('dashboard/profile/License')
					},
					{
						path: "connections",
						component: lazyLoad('dashboard/profile/connections/ConnectionsRoot'),
						redirect: '/dashboard/profile/settings',
						children: [
							{
								path: "patreon",
								component: lazyLoad('dashboard/profile/connections/Patreon')
							}
						]
					}
				]
			},
			{
				path: 'devices',
				component: lazyLoad('dashboard/devices/DevicesRoot')
			},
			{
				path: 'devices/:id/setup',
				component: lazyLoad('dashboard/devices/Setup/Setup')
			},
			{
				path: 'tokens',
				component: lazyLoad('dashboard/ApiTokens/ApiTokenRoot')
			},
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
