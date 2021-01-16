import { ShardingManager } from "discord.js-light";
import config from "config";
import polka from "polka";
import { AggregatorRegistry, collectDefaultMetrics, register } from "prom-client";

collectDefaultMetrics({ register });

(async () => {
  const shards = new ShardingManager("Shard/index.js", {
    token: config.get("token"),
    respawn: true,
    mode: "worker",
  });

  await shards.spawn();

  polka()
    .listen(9801)
    .get("/metrics", async (_, response) => {
      try {
        // collect all shard metrics
        const metrics: ReturnType<typeof register["getMetricsAsJSON"]>[] = await shards.broadcastEval(
          "require('prom-client').register.getMetricsAsJSON()"
        );

        // add our own metrics (sharding manager process)
        metrics.unshift(register.getMetricsAsJSON());

        // respond with aggregation
        const aggregate = AggregatorRegistry.aggregate(metrics);

        response.setHeader("Content-Type", aggregate.contentType);
        response.end(aggregate.metrics());
      } catch (e) {
        console.warn("could not export metrics", e);

        response.statusCode = 500;
        response.end(e.stack || e.message || "Internal Server Error");
      }
    });
})();
