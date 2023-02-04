﻿"use strict";

function EncryptMessage(message, key) {
    var iv = CryptoJS.lib.WordArray.random(25).toString();
    var key2 = CryptoJS.lib.WordArray.random(32).toString();

    var encrypted = CryptoJS.AES.encrypt(message, key2, { iv: iv }).toString();

    var context = {
        Text: encrypted,
        PrivateKey: key2,
        IV: iv
    };

    return context;
}

function DecryptMessage(context) {
    return CryptoJS.AES.decrypt(context.text, context.privateKey, { iv: context.iv }).toString(CryptoJS.enc.Utf8);
}

let token;
let username;

const hubConnection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub", { accessTokenFactory: () => token })
    .build();



const divs = document.querySelectorAll("div");
divs.forEach(function (div) {
    div.addEventListener("click", function () {
        if (this.getAttribute("data-email") != null) {
            username = this.getAttribute("data-email").toString();
        }
    });
});

document.getElementById("sendBtn").addEventListener("click", () => {
    const message = document.getElementById("message").value;

    let context = EncryptMessage(message);

    hubConnection.invoke("SendMessage", context, username)
        .catch(error => console.error(error));
});

// получение сообщения от сервера
hubConnection.on("ReceiveMessage", (context) => {
    let message = DecryptMessage(context);

    // создаем элемент <b> для имени пользователя
    const userNameElem = document.createElement("b");
    userNameElem.textContent = `User: `;

    // создает элемент <p> для сообщения пользователя
    const elem = document.createElement("p");
    elem.appendChild(userNameElem);
    elem.appendChild(document.createTextNode(message));

    var firstElem = document.getElementById("chatroom").firstChild;
    document.getElementById("chatting").insertBefore(elem, firstElem);
});



hubConnection.on("SendPrivateKeys", (key) => {
    let message = DecryptMessage(context);
});

hubConnection.start();