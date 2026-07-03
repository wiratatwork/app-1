import express from 'express';
import cors from 'cors';

const app = express();
const port = Number(process.env.PORT || 5002);

app.use(cors());

app.get('/health', (_req, res) => {
  res.json({ status: 'ok', app: 'app-1-api' });
});

app.get('/api/me', (_req, res) => {
  // Backend not implemented yet; frontend reads user from id_token.
  res.json({ message: 'Use Keycloak id_token claims in frontend for now.' });
});

app.listen(port, () => {
  console.log(`app-1 api listening on port ${port}`);
});
