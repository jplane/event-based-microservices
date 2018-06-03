
const express = require('express');
const mongoose = require('mongoose');
const uuid = require('uuidv4');
const axios = require('axios');
require('dotenv').config();

mongoose.connect(process.env.DB_CONNECTION, {
    auth: {
      user: process.env.DB_USER,
      password: process.env.DB_PWD
    }
  });

const maintenanceTicketSchema = new mongoose.Schema({
    id: 'string',
    roomId: 'number',
    start: 'date',
    reason: 'string'
});

const MaintenanceTicket = mongoose.model('MaintenanceTicket', maintenanceTicketSchema);

const app = express();

app.use(express.json());

app.post('/maintenance/create', (req, res) => {
    MaintenanceTicket.create({
        id: uuid(),
        roomId: req.body.roomId,
        start: req.body.start,
        reason: req.body.reason
    }, async (err, t) => {
        if (err) {
            res.status(500).send(err)
        } else {
            const evts = [{
                id: uuid(),
                eventType: 'maintenanceTicketCreated',
                subject: t.id,
                eventTime: new Date(),
                data: {
                    ticket: {
                        id: t.id,
                        roomId: t.roomId,
                        start: t.start,
                        reason: t.reason
                    }
                }
            }];
            const opts = {
                headers: {
                    'aeg-sas-key': process.env.AEG_SAS_KEY
                }
            };
            try {
                await axios.post(process.env.MAINTENANCE_TOPIC_ENDPOINT, evts, opts);
                res.send(t.id);
            } catch (e) {
                res.status(500).send(e.message);
            }
        }
    });
});

app.delete('/maintenance/remove/:id', (req, res) => {
    MaintenanceTicket.findOneAndDelete({
        id: req.params.id
    }, async (err, t) => {
        if (err) {
            res.status(500).send(err)
        } else {
            const evts = [{
                id: uuid(),
                eventType: 'maintenanceTicketCompleted',
                subject: t.id,
                eventTime: new Date(),
                data: {
                    ticket: {
                        id: t.id,
                        roomId: t.roomId,
                        start: t.start,
                        reason: t.reason
                    }
                }
            }];
            const opts = {
                headers: {
                    'aeg-sas-key': process.env.AEG_SAS_KEY
                }
            };
            try {
                await axios.post(process.env.MAINTENANCE_TOPIC_ENDPOINT, evts, opts);
                res.send(t.id);
            } catch (e) {
                res.status(500).send(e.message);
            }
        }
    });
});

app.listen(3000,
    () => console.log('App listening on port 3000'));
