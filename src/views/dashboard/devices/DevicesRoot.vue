<template>
  <div class="base-wrap">
    <div class="add-circle" @click="createNewDevice">
      <i class="fa-solid fa-plus"></i>
    </div>

    <b-container>
      <b-table hover striped :items="devices" :fields="fields" class="devices-table">
        <template #cell(status)="row">
          <span :class="!row.item.$onlineState ? 'offline' : 'online'">
            <i class="fa-solid fa-circle"></i>
          </span>
        </template>
        <template #cell(actions)="row">
          <div cols="auto" class="elli" @click="ellipsis($event, row.item)">
            <i class="fa-solid fa-ellipsis-vertical"></i>
          </div>
        </template>
      </b-table>
    </b-container>

    <b-modal v-model="modal.edit" title="Edit Device" ok-title="Save" @ok="applyEdits">
      <loading v-if="modal.editLoading"></loading>
      <div v-else>
        <b-container style="padding: 0;">
          <b-form-group label="ID (readonly)" label-for="item-id" label-class="mb-1">
            <b-form-input id="item-id" v-model="editItem.id" readonly />
          </b-form-group>

          <b-form-group label="Name (editable)" label-for="item-name" label-class="mb-1">
            <b-form-input id="item-name" v-model="editItem.name" />
          </b-form-group>

          <b-form-group label="Token (regeneratable)" label-for="item-token" label-class="mb-1">
            <b-form-input id="item-token" v-model="editItem.token" readonly />
          </b-form-group>

          <b-form-group label="Created on (readonly)" label-for="item-created" label-class="mb-1">
            <b-form-input id="item-created" v-model="editItem.createdOn" readonly />
          </b-form-group>
        </b-container>
      </div>
    </b-modal>
  </div>
</template>

<script>
import Loading from '../../utils/Loading.vue';

export default {
  components: { Loading },
  data() {
    return {
      fields: [
        {
          key: "status",
          thClass: "actions-header",
          label: ""
        },
        {
          key: "name"
        },
        {
          key: 'actions',
          thClass: "actions-header",
          label: ""
        }
      ],
      devices: [],
      modal: {
        edit: false,
        editLoading: false
      },
      editItem: {
        id: "",
        name: "",
        token: "",
        createdOn: ""
      }
    }
  },
  mounted() {
    this.$store.dispatch('setNewNav', []);
  },
  async beforeMount() {
    this.emitter.on('deviceStateUpdate', () => {
      this.updateOnlineStateAll();
    });

    await this.loadDevices();
  },
  methods: {
    async generatePairCode(item) {
      this.$swal({
        title: 'Generate Pair Code?',
        html: "Generate a pair code for this device.<br>Its vaild for <b>15 minutes</b> since its creation.<br>There is only one active pair code per device, newly generated ones will override the older active ones.",
        icon: 'info',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: 'var(--secondary-seperator-color)',
        showLoaderOnConfirm: true,
        reverseButtons: true,
        confirmButtonText: 'Get Pair Code',
        allowOutsideClick: () => !this.$swal.isLoading(),
        preConfirm: async () => {
          try {
            const res = await apiCall.makeCall('GET', `1/devices/${item.id}/pair`);
            if (res.status !== 200) {
              throw new Error(res.statusText);
            }

            return res.data;
          } catch (err) {
            this.$swal.showValidationMessage(`Request failed: ${err}`)
          }
        },
      }).then((result) => {
        console.log(result);
        if (result.isConfirmed) {
          let timerInterval
          this.$swal({
            title: 'Pair Code',
            html: 'Your pair code is<br><b style="font-size: 3rem">' + result.value.data + '</b><br>Expires in <expire></expire>',
            timer: 1000 * 60 * 15,
            timerProgressBar: true,
            didOpen: () => {
              const b = this.$swal.getHtmlContainer().querySelector('expire');
              timerInterval = setInterval(() => {
                const left = this.$swal.getTimerLeft();
                const minutes = Math.floor(left / 1000 / 60);
                b.textContent = `${minutes}:${Math.floor(left / 1000 - minutes * 60)}`;
              }, 1000)
            },
            willClose: () => {
              clearInterval(timerInterval);
            }
          });
        }
      });
    },
    regenerateToken(item) {
      this.$swal({
        title: 'Regenerate token?',
        html: "Your device token will be regenerated, this means the <b>previous one</b> is going to <b>invalid</b> from that point on. <br><br>Are you sure?",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: 'var(--secondary-seperator-color)',
        showLoaderOnConfirm: true,
        confirmButtonText: 'Regenerate Token',
        allowOutsideClick: () => !this.$swal.isLoading(),
        preConfirm: async () => {
          try {
            const res = await apiCall.makeCall('PUT', `1/devices/${item.id}`);
            if (res.status !== 200) {
              throw new Error(res.statusText);
            }

          } catch (err) {
            this.$swal.showValidationMessage(`Request failed: ${err}`)
          }
        },
      }).then((result) => {
        if (result.isConfirmed) {
          this.$swal('Success!', 'Successfully regenerated device token', 'success');
        }
      });
    },
    deleteDevice(item) {

      this.$swal({
        title: 'Delete?',
        html: `You are about to delete device <b>${item.name}</b> with id (${item.id}).<br>This will also delete <b>all shocker configurations and shares associated with that shocker.</b>
          <br><br><b><u>This is permanent and cannot be undone.</u></b><br>Are you sure?`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: 'var(--secondary-seperator-color)',
        showLoaderOnConfirm: true,
        confirmButtonText: 'Delete device',
        allowOutsideClick: () => !this.$swal.isLoading(),
        preConfirm: async () => {
          try {
            const res = await apiCall.makeCall('DELETE', `1/devices/${item.id}`);
            if (res.status !== 200) {
              throw new Error(res.statusText);
            }

          } catch (err) {
            this.$swal.showValidationMessage(`Request failed: ${err}`)
          }
        },
      }).then(async (result) => {
        if (result.isConfirmed) {
          this.$swal('Success!', 'Successfully deleted device', 'success');
          this.loadDevices();
        }
      });


    },
    async createNewDevice() {
      const res = await apiCall.makeCall('POST', '1/devices');
      if (res === undefined || res.status !== 201) {
        toastr.error("Error while creating new device");
        return;
      }

      this.$swal('Success!', 'Successfully created new device', 'success');
      await this.loadDevices();
    },
    getOnlineState(deviceId) {
      if (this.$store.state.deviceStates[deviceId] === undefined) return false;
      return this.$store.state.deviceStates[deviceId];
    },
    updateOnlineStateAll() {
      this.devices.forEach(device => {
        device.$onlineState = this.getOnlineState(device.id);
      });
    },
    async loadDevices() {
      const res = await apiCall.makeCall('GET', '1/devices');
      if (res === undefined || res.status !== 200) {
        toastr.error("Error while retrieving devices");
        return;
      }

      this.devices = res.data.data;
      this.updateOnlineStateAll();
    },
    async editDevice(item) {
      this.modal.editLoading = true;
      this.modal.edit = true;

      const res = await apiCall.makeCall('GET', '1/devices/' + item.id);
      if (res === undefined || res.status !== 200) {
        toastr.error("Error while retrieving device details");
        return;
      }

      this.editItem = res.data.data;
      this.modal.editLoading = false;
    },
    async applyEdits() {
      const res = await apiCall.makeCall('PATCH', '1/devices/' + this.editItem.id, {
        name: this.editItem.name
      });

      if (res === undefined || res.status !== 200) {
        toastr.error("Error while updating device");
        return;
      }

      this.$swal('Success!', `Successfully updated device [${this.editItem.id}]`, 'success');
      await this.loadDevices();
    },
    sendCaptiveMessage(deviceId, enabled) {
      ws.captive(deviceId, enabled);
    },
    ellipsis(e, item) {
      this.$contextmenu({
        theme: utils.isDarkMode() ? 'default dark' : 'default',
        x: e.x,
        y: e.y,
        items: [
          {
            label: "Edit",
            icon: 'fa-solid fa-pen-to-square',
            onClick: () => {
              this.editDevice(item);
            }
          },
          {
            label: "Regenerate Token",
            icon: 'fa-solid fa-rotate',
            onClick: () => {
              this.regenerateToken(item);
            }
          },
          {
            label: "Pair",
            icon: 'fa-solid fa-link',
            onClick: () => {
              this.generatePairCode(item);
            }
          },
          {
            label: "Remote Debug",
            children: [
              {
                label: "Captive Portal",
                icon: "fa-solid fa-pager",
                children: [
                  {
                    label: "On",
                    icon: "fa-solid fa-toggle-on",
                    onClick: () => {
                      this.sendCaptiveMessage(item.id, true);
                    }
                  },
                  {
                    label: "Off",
                    icon: "fa-solid fa-toggle-off",
                    onClick: () => {
                      this.sendCaptiveMessage(item.id, false);
                    }
                  }
                ]
              }
            ]
          },
          {
            label: "Setup",
            icon: 'fa-solid fa-layer-group',
            onClick: () => {
              this.$router.push(`/dashboard/devices/${item.id}/setup`);
            }
          },
          {
            label: "Delete",
            icon: 'fa-solid fa-trash',
            onClick: () => {
              this.deleteDevice(item);
            }
          }
        ]
      });
    },
  }
}
</script>

<style scoped lang="scss">
.add-circle {
  position: fixed;
  right: 10px;
  bottom: 10px;
  width: 60px;
  height: 60px;


  background-color: #7ac142;
  border-radius: 50%;
  cursor: pointer;

  transition: background-color 0.2s;

  &:hover {
    background-color: #5e9634;
  }

  svg {
    height: 40px;
    width: 40px;

    position: relative;
    left: 52%;
    transform: translateX(-50%) translateY(-50%);
    top: 50%;

  }
}

.elli {
  width: 24px;

  .fa-ellipsis-vertical {
    height: 24px;
    margin: auto;
    display: block;
  }
}

:deep(.actions-header) {
  width: 0px;
}

.devices-table {
  .mr {
    margin-right: 10px;

    --bs-btn-color: #fff;
    --bs-btn-hover-color: #fff;
    --bs-btn-active-color: #fff;
  }
}

.online {
  color: greenyellow;
}

.offline {
  color: red;
}
</style>