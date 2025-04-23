import { notify } from 'react-notify-toast';

class NotificationManager {
private notification : any

    showErrorNotify(message: string) {
        notify.show(message, 'error', 5000);
    }

    showWarnNotify(message: string) {
        notify.show(message, 'warning', 5000);
    }

    showSuccessNotify(message: string) {
        notify.show(message, 'success', 5000);
    }

    showCustomNotify(message: string) {
        let myColor = { background: '#54c7ec', text: "white" };
        notify.show(message, 'custom', 5000, myColor);
    }
    HideNotify(message: string) {
        notify.show(message, 'success', 3000);
    }
    PhotoshootNotify(message: string) {
        notify.show(message, 'success', 1000);
    }
    ImageHideNotify(message: string) {
        notify.show(message, 'success', 500);
    }
    showSuccessNotify1(message: string){
        this.notification = notify.show(message, 'success')
    }
    hideErrorNotify() {
        if (this.notification) {
            notify.hide();
        }
    }   
}

export default NotificationManager;
