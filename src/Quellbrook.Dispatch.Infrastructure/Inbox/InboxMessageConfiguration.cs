using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Quellbrook.Dispatch.Infrastructure.Inbox;

internal sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(message => message.MessageId);
        builder.Property(message => message.Type).HasMaxLength(100).IsRequired();
        builder.HasIndex(message => message.ProcessedAt);
    }
}
