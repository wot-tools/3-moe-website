class CreateTiers < ActiveRecord::Migration[5.0]
  def change
    create_table :tiers do |t|
		t.string :name, null: false
		t.integer :mark_count, null: false, default: 0

		t.timestamps
    end
  end
end
